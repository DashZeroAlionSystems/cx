using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.ACL;

public static class ACLServiceDI
{
    public const string ConfigurationSection = "ACLService";
    
    public static void AddACLService(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<ACLServiceOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<ACLService>();
    }
}