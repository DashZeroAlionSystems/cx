using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.Stores.Json;

public static class ConfigJsonStoreProviderDI
{
    public const string ConfigurationSection = "ConfigJsonStoreProvider";
    public const string ConfigurationTableName = "config_any";

    public static ConfigJsonStoreProviderOptions GetOptions(IConfiguration configuration) => configuration.GetRequiredSection<ConfigJsonStoreProviderOptions>(ConfigurationSection);

    public static void AddTypedJsonConfigTable<T>(this IServiceCollection sc, IConfiguration configuration, string section, string tableName) where T: class
    {
        sc.ConfigureNamedOptionsSection<T>(configuration, ConfigurationSection);
        sc.AddSingleton(new ConfigJsonStoreSource(section, tableName));
        
        sc.AddTransient<TypedJsonStore<T>>(sp => {
            var opts = sp.GetRequiredService<IOptions<ConfigJsonStoreProviderOptions>>();
            var store = new JsonStore(tableName, ConfigJsonStoreProvider.JsonStoreKeyLength, opts.Value.PostgreSQLClientName, sp);
            return new(store);
        });
    }

    public static void AddConfigJsonStoreProvider(this IServiceCollection services, ConfigurationManager configuration)
    {
        Directory.CreateDirectory(SecretsProvider.SecretsPath);
        File.WriteAllText(ConfigJsonStoreProvider.ConfigPath, "{}");
        configuration.AddJsonFile(ConfigJsonStoreProvider.ConfigPath, false, true);
        services.AddSingleton(new ConfigJsonStoreSource(null, ConfigurationTableName));
        
        services.Configure<ConfigJsonStoreProviderOptions>(configuration.GetSection(ConfigurationSection));
        services.AddSingleton<ConfigJsonStoreProvider>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ConfigJsonStoreProvider>());
    }
}