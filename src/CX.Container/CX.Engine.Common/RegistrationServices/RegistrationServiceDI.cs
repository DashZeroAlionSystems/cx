using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.RegistrationServices;

public static class RegistrationServiceDI
{
    public const string ConfigurationSectionName = "RegistrationService";
    
    public static void AddRegistrationService(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<RegistrationServiceOptions>(configuration.GetSection(ConfigurationSectionName));
    }
}