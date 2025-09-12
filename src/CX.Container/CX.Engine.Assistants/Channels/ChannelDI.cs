using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.Channels;

public static class ChannelDI
{
    public const string ConfigurationSection = "Channels";
    public const string ConfigurationTableName = "config_channels";
    
    public static void AddChannels(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<ChannelOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        
        sc.AddNamedSingletons<Channel>(configuration,
            (sp, config, name, optional) =>
            {
                IOptionsMonitor<ChannelOptions> opts;

                if (!config.SectionExists(ConfigurationSection, name))
                {
                    var assistant = sp.GetNamedService<IAssistant>(name);
                    var isImplicit = assistant != null;
                    if (isImplicit)
                    {
                        opts = config.MonitorSection<ChannelOptions>(ConfigurationSection, name, (section, opts) =>
                        {
                            if (!section.Exists())
                                opts.AssistantName = name;
                            
                            new JsonOptionsSetup<ChannelOptions>(section!).Configure(opts); });
                    }
                    else
                    {
                        if (optional)
                            return null;

                        opts = config.MonitorRequiredSection(ConfigurationSection, name, JsonOptionsSetup<ChannelOptions>.Factory);
                    }
                }
                else
                    opts = config.MonitorRequiredSection(ConfigurationSection, name, JsonOptionsSetup<ChannelOptions>.Factory);
                
                var logger = sp.GetLogger<Channel>(name);
                return new(opts, logger, sp);
            });
    }
}