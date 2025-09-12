using Aela.Server.Wrappers;
using CX.Container.Server.Resources;
using CX.Container.Server.Wrappers;

namespace CX.Container.Server.Extensions.Application
{
    public static class AiExtension
    {
        public static void RegisterAiExtension(this IServiceCollection services,
            IConfiguration configuration, IWebHostEnvironment env)
        {
            if (!env.IsEnvironment(Consts.Testing.FunctionalTestingEnvName))
            {
                services.AddSingleton<IAiServerTasks, AiServerTasks>();
            }
        }
    }
}
