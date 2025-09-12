using System.Data.Common;
using System.Net.Sockets;
using CX.Engine.Common.Db;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;

namespace CX.Engine.Common.PostgreSQL;

public class PostgreSQLClient : IDisposable
{
    private Snapshot _snapshot;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly IDisposable _optionsChangeDisposable;

    private class Snapshot
    {
        public readonly PostgreSQLClientOptions Options;
        public readonly NpgsqlDataSource DataSource;

        public Snapshot(PostgreSQLClientOptions options)
        {
            Options = options;
            var builder = new NpgsqlDataSourceBuilder(options.ConnectionString);
            DataSource = builder.Build();
        }
    }

    /// <summary>
    /// A semaphore for managing concurrency to the database.  If you wish to run direct queries, make sure to acquire the semaphore.
    /// </summary>
    public readonly SemaphoreSlim MaxConcurrencyLock = new(1, 1);

    public PostgreSQLClient(IOptionsMonitor<PostgreSQLClientOptions> options, ILogger logger, IServiceProvider sp)
    {
        _optionsChangeDisposable = options.Snapshot(() => _snapshot?.Options, o => _snapshot = new(o), logger, sp);

        var rpb = new ResiliencePipelineBuilder()
            // Define the retry policy using ResiliencePipelineBuilder
            .AddRetry(new()
            {
                ShouldHandle = ctx =>
                    ValueTask.FromResult(ctx.Outcome.Exception is NpgsqlException { IsTransient: true } or SocketException),
                MaxRetryAttempts = int.MaxValue,
                Delay = TimeSpan.FromSeconds(1),
                //1 2 4 8 16 30
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
    /// Does not automatically acquire <see cref="MaxConcurrencyLock"/>.
    /// </summary>
    public async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        var con = _snapshot.DataSource.CreateConnection();
        await con.OpenAsync();
        return con;
    }

    public async Task<NpgsqlTransaction> GetOpenTransactionAsync()
    {
        var con = await GetOpenConnectionAsync();
        var trans = await con.BeginTransactionAsync();
        return trans;
    }

    #region Execute

    public async Task<T> ExecuteAsync<T>([LanguageInjection("SQL")] string cmdString) => (T) await ExecuteAsync(cmdString);
    public async Task<object> ExecuteAsync([LanguageInjection("SQL")] string cmdString)
    {
        using var cmd = new NpgsqlCommand(cmdString);
        return await ExecuteAsync(cmd);
    }

    public async Task<T> ExecuteAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString) => (T) await ExecuteAsync(cmdString);
    public async Task<object> ExecuteAsync([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString, NpgsqlConnection con = null)
    {
        using var cmd = cmdString.GetCommand();
        return await ExecuteAsync(cmd, con);
    }
    
    public async Task<T> ExecuteAsync<T>(NpgsqlCommand cmd) => (T) await ExecuteAsync(cmd);

    public async Task<object> ExecuteAsync(NpgsqlCommand cmd, NpgsqlConnection con = null)
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
                    using var con = await GetOpenConnectionAsync();
                    cmd.Connection = con;
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
        using var cmd = new NpgsqlCommand(cmdString);
        return await ScalarAsync<T>(cmd);
    }

    public async Task<T> ScalarAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString)
    {
        using var cmd = cmdString.GetCommand();
        return await ScalarAsync<T>(cmd);
    }

    public async Task<T> ScalarAsync<T>(NpgsqlCommand cmd)
    {
        await MaxConcurrencyLock.WaitAsync();
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async _ =>
            {
                using var con = await GetOpenConnectionAsync();
                cmd.Connection = con;
                
                var rdr = await cmd.ExecuteReaderAsync();

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
    public async Task<(NpgsqlConnection con, NpgsqlCommand cmd, DbDataReader rdr)> ReaderAsync(string cmdString, NpgsqlConnection con = null)
    {
        var cmd = new NpgsqlCommand(cmdString);
        var (rdrCon, rdr) = await ReaderAsync(cmd, con);
        return (rdrCon, cmd, rdr);
    }

    /// <summary>
    /// Does not automatically acquire <see cref="MaxConcurrencyLock"/>.
    /// </summary>
    public async Task<(NpgsqlConnection con, NpgsqlCommand cmd, DbDataReader rdr)> ReaderAsync(NpgsqlCommandInterpolatedStringHandler cmdString, NpgsqlConnection con = null)
    {
        var cmd = cmdString.GetCommand();
        var (rdrCon, rdr) = await ReaderAsync(cmd, con);
        return (rdrCon, cmd, rdr);
    }

    /// <summary>
    /// Does not automatically acquire <see cref="MaxConcurrencyLock"/>.  Does not offer any resilience.
    /// </summary>
    public async Task<(NpgsqlConnection con, DbDataReader rdr)> ReaderAsync(NpgsqlCommand cmd, NpgsqlConnection con = null)
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
        using var cmd = new NpgsqlCommand(cmdString);
        return await RowAsync(cmd, map);
    }

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] string cmdString) where T : IPopulateFromDbDataReader, new()
    {
        using var cmd = new NpgsqlCommand(cmdString);

        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await RowAsync(cmd, Map);
    }

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString, Func<DbDataReader, T> map)
    {
        using var cmd = cmdString.GetCommand();
        return await RowAsync(cmd, map);
    }

    public async Task<T> RowAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString) where T : IPopulateFromDbDataReader, new()
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

    public async Task<T> RowAsync<T>(NpgsqlCommand cmd, Func<DbDataReader, T> map)
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

    public async Task<T> RowAsync<T>(NpgsqlCommand cmd) where T : IPopulateFromDbDataReader, new()
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

    #region ListDictionary
    public async Task<List<Dictionary<string, object>>> ListDictionaryAsync([LanguageInjection("SQL")] string cmdString, NpgsqlConnection con = null, List<string> skipColumns = null)
    {
        var cmd = new NpgsqlCommand(cmdString);
        return await ListAsync(cmd, r => r.ToDictionary(skipColumns), con);
    }

    public async Task<List<Dictionary<string, object>>> ListDictionaryAsync([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString, NpgsqlConnection con = null, List<string> skipColumns = null)
    {
        var cmd = cmdString.GetCommand();
        return await ListAsync(cmd, r => r.ToDictionary(skipColumns), con);
    }

    public Task<List<Dictionary<string, object>>> ListDictionaryAsync(NpgsqlCommand cmd, NpgsqlConnection con = null, List<string> skipColumns = null)
    {
        return ListAsync(cmd, r => r.ToDictionary(skipColumns), con);
    }

    #endregion
    
    #region ListString
    public async Task<List<string>> ListStringAsync([LanguageInjection("SQL")] string cmdString, NpgsqlConnection con = null)
    {
        var cmd = new NpgsqlCommand(cmdString);
        return await ListAsync(cmd, r => r.IsDBNull(0) ? null : r.GetString(0), con);
    }

    public async Task<List<string>> ListStringAsync([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString, NpgsqlConnection con = null)
    {
        var cmd = cmdString.GetCommand();
        return await ListAsync(cmd, r => r.IsDBNull(0) ? null : r.GetString(0), con);
    }

    public Task<List<string>> ListStringAsync(NpgsqlCommand cmd, NpgsqlConnection con = null) => ListAsync(cmd, r => r.IsDBNull(0) ? null : r.GetString(0), con);
    #endregion
    
    #region List<T>

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] string cmdString, Func<DbDataReader, T> map, NpgsqlConnection con = null)
    {
        var cmd = new NpgsqlCommand(cmdString);
        return await ListAsync(cmd, map, con);
    }

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] string cmdString, NpgsqlConnection con = null) where T : IPopulateFromDbDataReader, new()
    {
        var cmd = new NpgsqlCommand(cmdString);

        static T Map(DbDataReader r)
        {
            var t = new T();
            t.PopulateFromDbDataReader(r);
            return t;
        }

        return await ListAsync(cmd, Map, con);
    }

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString, Func<DbDataReader, T> map, NpgsqlConnection con = null)
    {
        var cmd = cmdString.GetCommand();
        return await ListAsync(cmd, map, con);
    }

    public async Task<List<T>> ListAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString, NpgsqlConnection con = null) where T : IPopulateFromDbDataReader, new()
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

    public async Task<List<T>> ListAsync<T>(NpgsqlCommand cmd, Func<DbDataReader, T> map, NpgsqlConnection con = null)
    {
        await MaxConcurrencyLock.WaitAsync();
        try
        {
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
        finally
        {
            MaxConcurrencyLock.Release();
        }
    }

    public async Task<List<T>> ListAsync<T>(NpgsqlCommand cmd) where T : IPopulateFromDbDataReader, new()
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
        using var cmd = new NpgsqlCommand(cmdString);
        return await RowDapperAsync<T>(cmd);
    }

    public async Task<T> RowDapperAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString) where T : new()
    {
        using var cmd = cmdString.GetCommand();
        return await RowDapperAsync<T>(cmd);
    }

    public async Task<T> RowDapperAsync<T>(NpgsqlCommand cmd) where T : new() => await RowAsync(cmd, rdr => rdr.Dapper<T>());

    #endregion

    #region ListDapper<T>

    public async Task<List<T>> ListDapperAsync<T>([LanguageInjection("SQL")] string cmdString) where T : new()
    {
        using var cmd = new NpgsqlCommand(cmdString);
        return await ListDapperAsync<T>(cmd);
    }

    public async Task<List<T>> ListDapperAsync<T>([LanguageInjection("SQL")] NpgsqlCommandInterpolatedStringHandler cmdString) where T : new()
    {
        using var cmd = cmdString.GetCommand();
        return await ListDapperAsync<T>(cmd);
    }

    public async Task<List<T>> ListDapperAsync<T>(NpgsqlCommand cmd) where T : new() => await ListAsync(cmd, rdr => rdr.Dapper<T>());

    #endregion

    public void Dispose()
    {
        _optionsChangeDisposable?.Dispose();
    }

    public async Task ExecuteInTransactionAsync(Func<NpgsqlTransaction, Task> action)
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

    public Task EnsurePgVectorEnabledAsync() => ExecuteAsync("CREATE EXTENSION IF NOT EXISTS vector");
}