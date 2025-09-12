using CX.Container.Domain.Duoporta;
using Microsoft.Extensions.DependencyInjection;
using CX.Engine.Common.Stores.Json;
using static CX.Container.Server.Extensions.Services.CXConsts;

namespace CX.Container.Server.Domain.Duoporta
{
    public static class DuoportaDI
    {
        public static IServiceCollection AddDuoportaServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DuoportaOptions>(configuration.GetSection("Duoporta"));
            return services;
        }
    }
}