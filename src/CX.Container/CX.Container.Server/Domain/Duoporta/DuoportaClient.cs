using CX.Engine.Common.Stores.Json;
using System.Text.Json;

namespace Aela.Server.Domain.Duoporta;

public interface IDuoportaClient
{
    Task<(string clientId, string apiKey, string baseUrl)> GetCredentialsAsync();
}

public class DuoportaClient : IDuoportaClient
{
    private readonly JsonStore _configStore;

    public DuoportaClient(JsonStore configStore)
    {
        _configStore = configStore;
    }

    public async Task<(string clientId, string apiKey, string baseUrl)> GetCredentialsAsync()
    {
        var config = await _configStore.GetAsync<JsonElement>("DuoportaClient");
        return (
            config.GetProperty("ClientId").GetString()!,
            config.GetProperty("ApiKey").GetString()!,
            config.GetProperty("BaseUrl").GetString()!
        );
    }
}