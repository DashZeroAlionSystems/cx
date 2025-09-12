using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Discord;

public static class DiscordServiceDI
{
    public const string ConfigurationSection = "DiscordServices";
    public const string ConfigurationTableName = "config_discord_services";

    public static void AddDiscordServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<DiscordServiceOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.Configure<DiscordServiceOptions>(configuration.GetSection(ConfigurationSection));

        sc.AddNamedSingletons<DiscordService>(configuration,
            static (sp, config, name, optional) =>
            {
                if (optional && !config.SectionExists(ConfigurationSection, name))
                    return null;
                
                var monitor = config.MonitorRequiredSection<DiscordServiceOptions>(ConfigurationSection, name);
                var logger = sp.GetLogger<DiscordService>(name);
                return new(monitor, logger, sp);
            });
    }
}