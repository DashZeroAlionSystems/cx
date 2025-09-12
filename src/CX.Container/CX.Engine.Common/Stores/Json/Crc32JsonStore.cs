using System.Text.Json;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;
using Npgsql;

namespace CX.Engine.Common.Stores.Json;

public class Crc32JsonStore
{
    private readonly IServiceProvider _sp;

    public Crc32JsonStore([NotNull] IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public struct StoreIdentifier
    {
        public PostgreSQLClient Client;
        public string ClientName;
        public string TableName;

        public StoreIdentifier(PostgreSQLClient client, string tableName)
        {
            Client = client;
            TableName = tableName;
        }

        public StoreIdentifier(string clientName, string tableName)
        {
            ClientName = clientName;
            TableName = tableName;
        }
        
        public static implicit operator StoreIdentifier((PostgreSQLClient client, string tableName) tuple) =>
            new(tuple.client, tuple.tableName);
        
        public static implicit operator StoreIdentifier((string clientName, string tableName) tuple) =>
            new(tuple.clientName, tuple.tableName);
    }

    public void ValidateResolveClient(ref StoreIdentifier storeId)
    {
        if (string.IsNullOrEmpty(storeId.TableName))
            throw new InvalidOperationException($"{nameof(StoreIdentifier.TableName)} is not set.");
        
        if (storeId.Client == null)
        {
            if (string.IsNullOrEmpty(storeId.ClientName))
                throw new InvalidOperationException($"{nameof(StoreIdentifier.ClientName)} is not set.");

            storeId.Client = _sp.GetRequiredNamedService<PostgreSQLClient>(storeId.ClientName);
        }
    }

    public async Task CreateTableIfNotExistsAsync(StoreIdentifier storeId)
    {
        ValidateResolveClient(ref storeId);
        await storeId.Client.ExecuteAsync(
            $"""
             CREATE TABLE IF NOT EXISTS {new InjectRaw(storeId.TableName)} (key_hash INT NOT NULL, key TEXT NOT NULL, value jsonb NOT NULL)
             """);
        
        //index on key_hash field
        await storeId.Client.ExecuteAsync(
            $"""
             CREATE INDEX IF NOT EXISTS {new InjectRaw($"{storeId.TableName}_key_hash_idx")} ON {new InjectRaw(storeId.TableName)} (key_hash)
             """);
    }

    public async Task<string> GetRawAsync(StoreIdentifier storeId, string key)
    {
        ValidateResolveClient(ref storeId);
        try
        {
            return await storeId.Client.ScalarAsync<string>(
                $"SELECT value FROM {new InjectRaw(storeId.TableName)} WHERE key = {key} AND key_hash = {key.GetCrc32()}");
        }
        catch (PostgresException e) when (e.Message.StartsWith("42P01"))
        {
            await CreateTableIfNotExistsAsync(storeId);
            return await GetRawAsync(storeId, key);
        }
    }
    
    public async Task<T> GetAsync<T>(StoreIdentifier storeId, string key)
    {
        var value = await GetRawAsync(storeId, key);
        if (value == null)
            return default;
        return JsonSerializer.Deserialize<T>(value);
    }
    
    public async Task ClearAsync(StoreIdentifier storeId)
    {
        ValidateResolveClient(ref storeId);
        try
        {
            await storeId.Client.ExecuteAsync(
                $"TRUNCATE TABLE {new InjectRaw(storeId.TableName)}");
        }
        catch (PostgresException e) when (e.Message.StartsWith("42P01"))
        {
            await CreateTableIfNotExistsAsync(storeId);
        }
    }
    
    public async Task RemoveAsync(StoreIdentifier storeId, string key)
    {
        ValidateResolveClient(ref storeId);

        try
        {
            await storeId.Client.ExecuteAsync(
                $"DELETE FROM {new InjectRaw(storeId.TableName)} WHERE key = {key} AND key_hash = {key.GetCrc32()}");
        }
        catch (PostgresException e) when (e.Message.StartsWith("42P01"))
        {
            await CreateTableIfNotExistsAsync(storeId);
        }
    }

    public async Task<string> SetRawAsync(StoreIdentifier storeId, string key, string value)
    {
        ValidateResolveClient(ref storeId);
        try
        {
            var affectedRows = await storeId.Client.ScalarAsync<int>(
                $"""
                 UPDATE {new InjectRaw(storeId.TableName)}
                 SET value = {value}::jsonb
                 WHERE key_hash = {key.GetCrc32()} AND key = {key}
                 """
            );

            if (affectedRows == 0)
            {
                await storeId.Client.ExecuteAsync(
                    $"""
                     INSERT INTO {new InjectRaw(storeId.TableName)} (key_hash, key, value)
                     VALUES ({key.GetCrc32()}, {key}, {value}::jsonb)
                     """
                );
            }
        }
        catch (PostgresException e) when (e.Message.StartsWith("42P01"))
        {
            await CreateTableIfNotExistsAsync(storeId);
            return await SetRawAsync(storeId, key, value);
        }

        return value;
    }


    public async Task SetAsync<T>(StoreIdentifier storeId, string key, T value)
    {
        var s = value == null ? null : JsonSerializer.Serialize(value);
        await SetRawAsync(storeId, key, s);
    }
}