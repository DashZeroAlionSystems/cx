using CX.Engine.ChatAgents;
using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.QAndA.Auto;

public static class AutoQADI
{
    public static void AddAutoQAs(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<AutoQA>(configuration, static (sp, config, name, optional) => {
            if (optional && !config.SectionExists("AutoQAs", name))
                return null;
            
            var options = config.GetRequiredSection("AutoQAs").GetRequiredSection<AutoQAOptions>(name);
            return new (options, sp.GetRequiredService<ChatCache>(), sp);
        });
    }
}