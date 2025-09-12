using System.Data.Common;
using System.Globalization;
using CX.Engine.Common.Db;
using CX.Engine.Common.PostgreSQL;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CX.Engine.Common.Stores.Binary.PostgreSQL;

public sealed class PostgreSQLBinaryStore : IBinaryStore
{
    private readonly PostgreSQLClient _sql;
    private readonly PostgreSQLBinaryStoreOptions _options;
    private readonly ILogger _logger;
    public readonly Task InitCompleteTask;

    public PostgreSQLBinaryStore(PostgreSQLBinaryStoreOptions options, ILogger logger, IServiceProvider sp)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _options.Validate();
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>(_options.PostgreSQLClientName);
        InitCompleteTask = InitAsync();
    }

    private async Task InitAsync()
    {
        _logger.LogInformation($"Initializing PostgreSQL Binary store with table {_options.TableName}...");
        await _sql.ExecuteAsync($"""
                            CREATE TABLE IF NOT EXISTS {new InjectRaw(_options.TableName)} (
                                key VARCHAR({new InjectRaw(_options.KeyLength.ToString(CultureInfo.InvariantCulture))}) PRIMARY KEY,
                                value bytea
                            );
                            """);
        _logger.LogInformation($"Initialized PostgreSQL Binary store with table {_options.TableName}.");
    }

    public async Task DeleteAsync(string key)
    {
        await InitCompleteTask;
        await _sql.ExecuteAsync($"""
                            DELETE FROM {new InjectRaw(_options.TableName)}
                            WHERE key = {key}
                            """);
    }

    public async Task ClearAsync()
    {
        await InitCompleteTask;
        await _sql.ExecuteAsync($"""
                            DELETE FROM {new InjectRaw(_options.TableName)}
                            """);
    }

    public async Task<List<BinaryStoreRow>> GetAllAsync()
    {
        await InitCompleteTask;
        return await _sql.ListAsync(
            $"""
             SELECT key, value
             FROM {new InjectRaw(_options.TableName)}
             """,
            MapRow);
    }

    public static BinaryStoreRow MapRow(DbDataReader rdr) => new(rdr.GetString(0), rdr.GetBytes(1));

    public async Task<byte[]> GetBytesAsync(string key)
    {
        await InitCompleteTask;
        return await _sql.ScalarAsync<byte[]>(
            $"""
             SELECT value
             FROM {new InjectRaw(_options.TableName)}
             WHERE key = {key}
             """);
    }

    public async Task<Stream> GetStreamAsync(string key)
    {
        var bytes = await GetBytesAsync(key);
        return bytes == null ? null : new MemoryStream(bytes);
    }

    public async Task SetBytesAsync(string key, byte[] value)
    {
        await InitCompleteTask;
        while (true)
        {
            try
            {
                await _sql.ExecuteAsync(
                    $"""
                     INSERT INTO {new InjectRaw(_options.TableName)} (key, value)
                     VALUES ({key}, {value})
                     ON CONFLICT (key) DO UPDATE
                     SET value = {value}
                     """);
                break;
            }
            //Deal with race conditions with ON CONFLICT (when two transactions try to insert at once, one will still throw this but will succeed on retry).
            catch (PostgresException ex)
            {
                if (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                    continue;
                throw;
            }
        }
    }

   public async Task<bool> TryChangeAsync(string key, byte[] oldValue, byte[] value)
    {
        await InitCompleteTask;
        try
        {
            await _sql.ExecuteAsync(
                $"""
                 WITH upsert AS (
                     UPDATE {new InjectRaw(_options.TableName)}
                     SET value = {value}
                     WHERE key = {key} AND ((value = {oldValue}) or (value is NULL and {oldValue} is NULL))
                     RETURNING *
                 )
                 INSERT INTO {new InjectRaw(_options.TableName)} (key, value)
                 SELECT {key}, {value}
                 WHERE NOT EXISTS (SELECT 1 FROM upsert)
                 """);

            return true;
        }
        catch (PostgresException ex)
        {
            if (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                return false;

            throw;
        }
    }
}