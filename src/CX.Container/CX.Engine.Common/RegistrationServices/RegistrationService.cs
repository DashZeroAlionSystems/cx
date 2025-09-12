using CX.Engine.Common.DistributedLocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.RegistrationServices;

public static class RegistrationService
{
    public static event Action<IHost> AfterHostBuild;
    public static event Action<IServiceCollection, ConfigurationManager> ConfigureServices;
    public static readonly List<StartupTaskFunc> StartupTasks = new();
    
    public static void InvokeAfterHostBuild(IHost host)
    {
        AfterHostBuild?.Invoke(host);
    }
    
    public static void InvokeConfigureServices(IServiceCollection services, ConfigurationManager configuration)
    {
        ConfigureServices?.Invoke(services, configuration);
    }

    public static void AddCXEngine(this IServiceCollection services, ConfigurationManager configuration) => InvokeConfigureServices(services, configuration);

    public static async Task StartCXEngineAsync(this IHost host)
    {
        InvokeAfterHostBuild(host);

        var lockService = host.Services.GetService<DistributedLockService>();
        
        if (lockService != null)
            await lockService.StartAsync(CancellationToken.None);
        
        foreach (var task in StartupTasks)
            await task(host);
        
        var opts = host.Services.GetService<IOptions<RegistrationServiceOptions>>();
        var logger = host.Services.GetLogger(typeof(RegistrationService).FullName);

        if (opts?.Value is { StartupTasks: not null, LuaCore: not null } )
        {
            var luaCore = host.Services.GetRequiredNamedService<LuaCore>(opts.Value.LuaCore);
            foreach (var task in opts.Value.StartupTasks)
            {
                try
                {
                    logger.LogInformation(await luaCore.RunAsync(task));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error running startup task {Task}", task);
                }
            }
        }
    }

    public static void AddRoute<T>(string routeName, Func<string, IServiceProvider, IConfiguration, bool, T> factory)
    {
        AfterHostBuild += host => host.Services.AddRoute(routeName, factory);
    }
}