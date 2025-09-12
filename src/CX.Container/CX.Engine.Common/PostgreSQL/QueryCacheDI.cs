using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.PostgreSQL;

public static class QueryCacheDI
{
    public static void AddQueryCache(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddSingleton<QueryCache>();
        sc.Configure<QueryCacheOptions>(configuration.GetSection("QueryCache"));
    }
}