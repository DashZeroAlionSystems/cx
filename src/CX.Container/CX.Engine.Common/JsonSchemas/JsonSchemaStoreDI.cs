using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.JsonSchemas;

public static class JsonSchemaStoreDI
{
    public const string ConfigurationSection = "JsonSchemaStore";
    public const string ConfigurationSectionSchemas = "JsonSchemas";
    public const string ConfigurationTableForSchemas = "config_jsonschemas";

    public static IOptionsMonitor<JsonSchemaOptions> MonitorSchema(string key, IConfiguration config) => config.MonitorRequiredSectionE(ConfigurationSectionSchemas, key, section => new JsonSchemaOptionsSetup(section)).monitor;

    public static void AddJsonSchemaStore(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<JsonSchemaStoreOptions>(configuration, ConfigurationSection, ConfigurationTableForSchemas);

        sc.AddSingleton<JsonSchemaStore>();
        sc.Configure<JsonSchemaStoreOptions>(configuration.GetSection(ConfigurationSection));
    }
}