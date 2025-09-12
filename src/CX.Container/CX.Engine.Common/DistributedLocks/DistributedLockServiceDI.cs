using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.Common.DistributedLocks;

public static class DistributedLockServiceDI
{
    public const string ConfigurationSection = "DistributedLockService";
    
    public static void AddDistributedLockService(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<DistributedLockServiceOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<DistributedLockService>();
        sc.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DistributedLockService>());
    }
}