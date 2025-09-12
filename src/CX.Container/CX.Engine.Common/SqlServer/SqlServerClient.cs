using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using CX.Engine.Common.Db;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace CX.Engine.Common.SqlServer;
public class SqlServerClient : IDisposable
{
    private Snapshot _snapshot;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly IDisposable _optionsChangeDisposable;

    private class Snapshot
    {
        public readonly SqlServerClientOptions Options;

        public Snapshot(SqlServerClientOptions options)
        {
            Options = options;
        }
    }

    /// <summary>
    /// A semaphore for managing concurrency to the database.  If you wish to run direct queries, make sure to acquire the semaphore.
    /// </summary>
    public readonly SemaphoreSlim MaxConcurrencyLock = new(1, 1);

    public SqlServerClient(IOptionsMonitor<SqlServerClientOptions> options, ILogger logger, IServiceProvider sp)
    {
        // Assume Snapshot extension works similarly to your PostgreSQL version.
        _optionsChangeDisposable = options.Snapshot(() => _snapshot?.Options, o => _snapshot = new(o), logger, sp);

        var rpb = new ResiliencePipelineBuilder()
            // Define the retry policy using ResiliencePipelineBuilder.
            .AddRetry(new()
            {
                ShouldHandle = ctx =>
                    // For SQL Server we consider SqlException (and SocketException) as transient.
                    ValueTask.FromResult(ctx.Outcome.Exception is SqlException or SocketException),
                MaxRetryAttempts = int.MaxValue,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "Retrying database operation");
                    return ValueTask.CompletedTask;
                }
            });
        _resiliencePipeline = rpb.Build();
    }

    /// <summary>
    /// Gets an open SQL connection.
    /// </summary>
    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var con = new SqlConnection(_snapshot.Options.ConnectionString);
        await con.OpenAsync();
        return con;
    }

    public async Task<DbTransaction> GetOpenTransactionAsync()
    {
        var con = await GetOpenConnectionAsync();
        return await con.BeginTransactionAsync();
    }

    #region Execute

    public async Task<T> ExecuteAsync<T>([LanguageInjection("SQL")] string cmdString) => (T)await ExecuteAsync(cmdString);

    public async Task<object> ExecuteAsync([LanguageInjection("SQL")] string cmdString)
    {
        using var cmd = new SqlCommand(cmdString);
        return await ExecuteAsync(cmd);
    }

    public async Task<T> ExecuteAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString) => (T)await ExecuteAsync(cmdString);

    public async Task<object> ExecuteAsync([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString, SqlConnection con = null)
    {
        using var cmd = cmdString.GetCommand();
        return await ExecuteAsync(cmd, con);
    }

    public async Task<T> ExecuteAsync<T>(SqlCommand cmd) => (T)await ExecuteAsync(cmd);

    public async Task<object> ExecuteAsync(SqlCommand cmd, SqlConnection con = null)
    {
        await MaxConcurrencyLock.WaitAsync();
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async _ =>
            {
                if (con != null)
                {
                    cmd.Connection = con;
                    return await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    using var localCon = await GetOpenConnectionAsync();
                    cmd.Connection = localCon;
                    return await cmd.ExecuteNonQueryAsync();
                }
            });
        }
        finally
        {
            MaxConcurrencyLock.Release();
        }
    }

    #endregion

    #region Scalar<T>

    public async Task<T> ScalarAsync<T>([LanguageInjection("SQL")] string cmdString)
    {
        using var cmd = new SqlCommand(cmdString);
        return await ScalarAsync<T>(cmd);
    }

    public async Task<T> ScalarAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString)
    {
        using var cmd = cmdString.GetCommand();
        return await ScalarAsync<T>(cmd);
    }

    public async Task<T> ScalarAsync<T>(SqlCommand cmd)
    {
        await MaxConcurrencyLock.WaitAsync();
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async _ =>
            {
                using var con = await GetOpenConnectionAsync();
                cmd.Connection = con;

                using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    var res = rdr.GetFieldValue<T>(0);
                    return res;
                }
                else
                    return default;
            });
        }
        finally
        {
            MaxConcurrencyLock.Release();
        }
    }

    #endregion

    #region Reader

    /// <summary>
    /// Does not automatically acquire <see cref="MaxConcurrencyLock"/>.
    /// </summary>
    public async Task<(SqlConnection con, SqlCommand cmd, DbDataReader rdr)> ReaderAsync(string cmdString, SqlConnection con = null)
    {
        var cmd = new SqlCommand(cmdString);
        var (rdrCon, rdr) = await ReaderAsync(cmd, con);
        return (rdrCon, cmd, rdr);
    }

    /// <summary>
    /// Does not automatically acquire <see cref="MaxConcurrencyLock"/>.
    /// </summary>
    public async Task<(SqlConnection con, SqlCommand cmd, DbDataReader rdr)> ReaderAsync(SqlServerCommandInterpolatedStringHandler cmdString, SqlConnection con = null)
    {
        var cmd = cmdString.GetCommand();
        var (rdrCon, rdr) = await ReaderAsync(cmd, con);
        return (rdrCon, cmd, rdr);
    }

    /// <summary>
    /// Does not automatically acquire <see cref="MaxConcurrencyLock"/>. Does not offer any resilience.
    /// </summary>
    public async Task<(SqlConnection con, DbDataReader rdr)> ReaderAsync(SqlCommand cmd, SqlConnection con = null)
    {
        if (con != null)
        {
            cmd.Connection = con;
            return (con, await cmd.ExecuteReaderAsync());
        }
        else
        {
            con = await GetOpenConnectionAsync();
            cmd.Connection = con;
            return (con, await cmd.ExecuteReaderAsync());
        }
    }

    #endregion

    #region Row<T>

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] string cmdString, Func<DbDataReader, T> map)
    {
        using var cmd = new SqlCommand(cmdString);
        return await RowAsync(cmd, map);
    }

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] string cmdString) where T : IPopulateFromDbDataReader, new()
    {
        using var cmd = new SqlCommand(cmdString);

        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await RowAsync(cmd, Map);
    }

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString, Func<DbDataReader, T> map)
    {
        using var cmd = cmdString.GetCommand();
        return await RowAsync(cmd, map);
    }

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString) where T : IPopulateFromDbDataReader, new()
    {
        using var cmd = cmdString.GetCommand();

        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await RowAsync(cmd, Map);
    }

    public async Task<T> RowAsync<T>(SqlCommand cmd, Func<DbDataReader, T> map)
    {
        await MaxConcurrencyLock.WaitAsync();
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async _ =>
            {
                using var con = await GetOpenConnectionAsync();
                cmd.Connection = con;
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return default;
                return map(reader);
            });
        }
        finally
        {
            MaxConcurrencyLock.Release();
        }
    }

    public async Task<T> RowAsync<T>(SqlCommand cmd) where T : IPopulateFromDbDataReader, new()
    {
        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await RowAsync(cmd, Map);
    }

    #endregion

    #region ListString

    public async Task<List<string>> ListStringAsync([LanguageInjection("SQL")] string cmdString, SqlConnection con = null)
    {
        var cmd = new SqlCommand(cmdString);
        return await ListAsync(cmd, r => r.IsDBNull(0) ? null : r.GetString(0), con);
    }

    public async Task<List<string>> ListStringAsync([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString, SqlConnection con = null)
    {
        var cmd = cmdString.GetCommand();
        return await ListAsync(cmd, r => r.IsDBNull(0) ? null : r.GetString(0), con);
    }

    public Task<List<string>> ListStringAsync(SqlCommand cmd, SqlConnection con = null) =>
        ListAsync(cmd, r => r.IsDBNull(0) ? null : r.GetString(0), con);

    #endregion

    #region List<T>

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] string cmdString, Func<DbDataReader, T> map, SqlConnection con = null)
    {
        var cmd = new SqlCommand(cmdString);
        return await ListAsync(cmd, map, con);
    }

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] string cmdString, SqlConnection con = null) where T : IPopulateFromDbDataReader, new()
    {
        var cmd = new SqlCommand(cmdString);

        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await ListAsync(cmd, Map, con);
    }

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString, Func<DbDataReader, T> map, SqlConnection con = null)
    {
        var cmd = cmdString.GetCommand();
        return await ListAsync(cmd, map, con);
    }

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString, SqlConnection con = null) where T : IPopulateFromDbDataReader, new()
    {
        var cmd = cmdString.GetCommand();

        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await ListAsync(cmd, Map, con);
    }

    public async Task<List<T>> ListAsync<T>(SqlCommand cmd, Func<DbDataReader, T> map, SqlConnection con = null)
    {
        using var _ = await MaxConcurrencyLock.UseAsync();
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var (rdrCon, rdr) = await ReaderAsync(cmd, con);

            var lst = new List<T>();

            try
            {
                while (await rdr.ReadAsync())
                    lst.Add(map(rdr));
            }
            finally
            {
                rdr.Dispose();
                cmd.Dispose();
                if (rdrCon != con)
                    rdrCon.Dispose();
            }

            return lst;
        });
    }

    public async Task<List<T>> ListAsync<T>(SqlCommand cmd) where T : IPopulateFromDbDataReader, new()
    {
        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await ListAsync(cmd, Map);
    }

    #endregion

    #region RowDapper<T>

    public async Task<T> RowDapperAsync<T>([LanguageInjection("SQL")] string cmdString) where T : new()
    {
        using var cmd = new SqlCommand(cmdString);
        return await RowDapperAsync<T>(cmd);
    }

    public async Task<T> RowDapperAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString) where T : new()
    {
        using var cmd = cmdString.GetCommand();
        return await RowDapperAsync<T>(cmd);
    }

    public async Task<T> RowDapperAsync<T>(SqlCommand cmd) where T : new() =>
        await RowAsync(cmd, rdr => rdr.Dapper<T>());

    #endregion

    #region ListDapper<T>

    public async Task<List<T>> ListDapperAsync<T>([LanguageInjection("SQL")] string cmdString) where T : new()
    {
        using var cmd = new SqlCommand(cmdString);
        return await ListDapperAsync<T>(cmd);
    }

    public async Task<List<T>> ListDapperAsync<T>([LanguageInjection("SQL")] SqlServerCommandInterpolatedStringHandler cmdString) where T : new()
    {
        using var cmd = cmdString.GetCommand();
        return await ListDapperAsync<T>(cmd);
    }

    public async Task<List<T>> ListDapperAsync<T>(SqlCommand cmd) where T : new() =>
        await ListAsync(cmd, rdr => rdr.Dapper<T>());

    #endregion

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }

    public async Task ExecuteInTransactionAsync(Func<DbTransaction, Task> action)
    {
        if (action == null)
            return;

        var trans = await GetOpenTransactionAsync();
        var con = trans.Connection;
        try
        {
            try
            {
                await action(trans);
            }
            catch (Exception)
            {
                await trans.RollbackAsync();
                throw;
            }

            await trans.CommitAsync();
        }
        finally
        {
            trans.Dispose();
            con?.Dispose();
        }
    }

    public async Task<QueryFactory> GetQueryFactory()
    {
        var con = await GetOpenConnectionAsync();
        var factory = new QueryFactory(con, new SqlServerCompiler());
        return factory;
    }
}