using CX.Engine.Common.Stores.Binary.Disk;
using CX.Engine.Common.Stores.Binary.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores.Binary;

public static class BinaryStoreDI
{
    public const string BinaryStore_none = "none";
    public const string BinaryStore_disk = "disk";
    public const string BinaryStore_postgresql = "postgresql";
    
    public static void AddBinaryStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddSingleton<NoBinaryStore>();
        sc.AddPostgreSQLBinaryStores(configuration);
        sc.AddDiskBinaryStores(configuration);

        sc.AddNamedSingletons<IBinaryStore>(configuration, (sp, _, name, optional) => {
            var (engine, subName) = name.SplitAtFirst(".");
            
            switch (engine)
            {
                case BinaryStore_none: return sp.GetService<NoBinaryStore>(optional);
                case BinaryStore_disk: return sp.GetNamedService<DiskBinaryStore>(subName, optional);
                case BinaryStore_postgresql: return sp.GetNamedService<PostgreSQLBinaryStore>(subName, optional);
                default:
                    throw new NotSupportedException($"Unsupported binary store engine: {engine}");
            }
        });
    }
}