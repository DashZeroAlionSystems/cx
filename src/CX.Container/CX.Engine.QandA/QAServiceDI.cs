using CX.Engine.Assistants;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using CX.Engine.FileServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CX.Engine.QAndA;

public static class QAServiceDI
{
    public const string ConfigurationSection = "QAServices";
    public const string ConfigurationTableName = "config_qa_services";

    public static void AddQAServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<QAServiceOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.Configure<QAServiceOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddNamedTransients<QAService>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            IOptionsMonitor<QAServiceOptions> opts;

            if (!config.SectionExists(ConfigurationSection, name))
            {
                var assistant = sp.GetNamedService<IAssistant>(name);
                var isImplicit = assistant != null;
                if (isImplicit)
                {
                    opts = config.MonitorSection<QAServiceOptions>(ConfigurationSection, name, (section, opts) =>
                    {
                        if (!section.Exists())
                            opts.AssistantName = name;
                    });
                }
                else
                {
                    if (optional)
                        return null;

                    opts = config.MonitorRequiredSection<QAServiceOptions>(ConfigurationSection, name);
                }
            }
            else
                opts = config.MonitorRequiredSection<QAServiceOptions>(ConfigurationSection, name);

            var logger = sp.GetLogger<QAService>(name);
            return new(name, opts, logger, sp, sp.GetService<ChatCache>(), sp.GetService<FileService>());
        });
    }
}