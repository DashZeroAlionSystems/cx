using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CX.Engine.DemoConsole;

public static class MoonyApiServiceDI
{
    public static void AddMoonyApiService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MoonyApiServiceOptions>(configuration.GetSection("MoonyApiService"));
        services.AddSingleton<MoonyApiService>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MoonyApiService>());
    }
}