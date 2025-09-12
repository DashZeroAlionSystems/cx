using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CX.Engine.Common.PostgreSQL;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CX.Engine.Common.Stores.Json;

public class JsonStore : IJsonStore
{
    private readonly PostgreSQLClient _sql;
    private readonly JsonStoreOptions _options;
    private readonly ILogger _logger;
    public readonly Task InitCompleteTask;

    public JsonStore(string tableName, int keyLength, string postgresqlClientName, IServiceProvider sp)
    {
        _logger = sp.GetLogger<JsonStore>(tableName);
        _options = new()
        {
            TableName = tableName,
            PostgreSQLClientName = postgresqlClientName,
            KeyLength = keyLength
        };
        _options.Validate();
        
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>(_options.PostgreSQLClientName);
        InitCompleteTask = InitAsync();
    }

    public JsonStore(JsonStoreOptions options, ILogger logger, IServiceProvider sp)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _options.Validate();
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>(_options.PostgreSQLClientName);
        InitCompleteTask = InitAsync();
    }

    private async Task InitAsync()
    {
        _logger.LogInformation($"Initializing JSON store with table {_options.TableName}...");
        await _sql.ExecuteAsync($"""
                            CREATE TABLE IF NOT EXISTS {new InjectRaw(_options.TableName)} (
                                key VARCHAR({new InjectRaw(_options.KeyLength.ToString(CultureInfo.InvariantCulture))}) PRIMARY KEY,
                                value JSONB
                            );
                            """);
        _logger.LogInformation($"Initialized JSON store with table {_options.TableName}.");
    }

    public async Task DeleteAsync(string key)
    {
        await InitCompleteTask;
        await _sql.ExecuteAsync($"""
                            DELETE FROM {new InjectRaw(_options.TableName)}
                            WHERE key = {key};
                            """);
    }

    public async Task ClearAsync()
    {
        await InitCompleteTask;
        await _sql.ExecuteAsync($"""
                            DELETE FROM {new InjectRaw(_options.TableName)};
                            """);
    }

    public async Task<List<Row>> GetAllAsync()
    {
        await InitCompleteTask;
        return await _sql.ListAsync(
            $"""
             SELECT key, value
             FROM {new InjectRaw(_options.TableName)};
             """,
            MapRow);
    }

    public readonly record struct Row(string Key, string Value);

    public static Row MapRow(DbDataReader rdr) => new(rdr.GetString(0), rdr.IsDBNull(1) ? null : rdr.GetString(1));

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await GetRawAsync(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
    
    public async Task<string> GetRawAsync(string key)
    {
        await InitCompleteTask;
        var bytes = await _sql.ScalarAsync<byte[]>(
            $"""
             SELECT value
             FROM {new InjectRaw(_options.TableName)}
             WHERE key = {key};
             """);
        return bytes == null ? null : Encoding.UTF8.GetString(bytes);
    }
    
    public async Task SetAsync<T>(string key, T value) => await SetRawAsync(key, JsonSerializer.Serialize(value));
    
    public async Task SetIfNotExistsAsync<T>(string key, T value) => await SetRawIfNotExistsAsync(key, JsonSerializer.Serialize(value));

    public async Task SetRawAsync(string key, string value)
    {
        await InitCompleteTask;
        while (true)
        {
            try
            {
                await _sql.ExecuteAsync(
                    $"""
                     INSERT INTO {new InjectRaw(_options.TableName)} (key, value)
                     VALUES ({key}, {value}::jsonb)
                     ON CONFLICT (key) DO UPDATE
                     SET value = {value}::jsonb;
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
    
    public async Task SetRawIfNotExistsAsync(string key, string value)
    {
        await InitCompleteTask;
        try
        {
            await _sql.ExecuteAsync(
                $"""
                 INSERT INTO {new InjectRaw(_options.TableName)} (key, value)
                 SELECT {key}, {value}::jsonb
                 ON CONFLICT (key) DO NOTHING;
                 """);
        }
        catch (PostgresException ex)
        {
            if (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                return;
            
            throw;
        }
    }

    public async Task<bool> TryChangeAsync<T>(string key, T oldValue, T newValue) => await TryChangeAsync(key, oldValue == null ? null : JsonSerializer.Serialize(oldValue), newValue == null ? null : JsonSerializer.Serialize(newValue));

    public async Task<bool> TryChangeAsync(string key, string oldValue, string value)
    {
        await InitCompleteTask;
        try
        {
            await _sql.ExecuteAsync(
                $"""
                 WITH upsert AS (
                     UPDATE {new InjectRaw(_options.TableName)}
                     SET value = {value}::jsonb
                     WHERE key = {key} AND ((value = {oldValue}::jsonb) or (value is NULL and {oldValue} is NULL))
                     RETURNING *
                 )
                 INSERT INTO {new InjectRaw(_options.TableName)} (key, value)
                 SELECT {key}, {value}::jsonb
                 WHERE NOT EXISTS (SELECT 1 FROM upsert);
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

public class JsonStore<T> : JsonStore, IJsonStore<T>
{
    public JsonStore(JsonStoreOptions options, ILogger logger, IServiceProvider sp) : base(options, logger, sp) { }

    public async Task<T> GetAsync(string key) => await GetAsync<T>(key);

    public async Task SetAsync(string key, T value) => await SetAsync<T>(key, value);

    public async Task<bool> TryChangeAsync(string key, T oldValue, T newValue) => await TryChangeAsync<T>(key, oldValue, newValue);
}