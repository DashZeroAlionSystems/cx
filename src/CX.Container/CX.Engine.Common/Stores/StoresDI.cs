using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores;

public static class StoresDI
{
    public static void AddStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddJsonStores(configuration);
        sc.AddBinaryStores(configuration);
    }
}