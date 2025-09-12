using CX.Engine.Common;
using CX.Engine.Common.RegistrationServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;

namespace CX.Engine.Assistants.Options;

public static class OptionsAssistantsDI
{
    public static void AddOptionsAssistants(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Walter1OptionsAssistantOptions>(configuration.GetSection("Walter1OptionsAssistant"));
        services.Configure<ChannelOptionsAssistantOptions>(configuration.GetSection("ChannelOptionsAssistant"));
        services.AddSingleton<ChannelOptionsAssistant>();
        services.AddSingleton<Walter1OptionsAssistant>();
        RegistrationService.AfterHostBuild += host => host.Services.AddRoute<IAssistant>("options", 
            (name, sp, _, optional) =>
            {
                switch (name)
                {
                    case "channels":
                        return sp.GetService<ChannelOptionsAssistant>(optional);
                    case "walter-1":
                        return sp.GetService<Walter1OptionsAssistant>(optional);
                    default:
                        if (optional)
                            return null;
                        else
                            throw new InvalidOperationException($"Unknown Options assistant name: {name}");
                }
            });
    }
}