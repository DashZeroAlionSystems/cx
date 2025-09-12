using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Stores.Binary.Disk;

public static class DiskBinaryStoreDI
{
    public static void AddDiskBinaryStores(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<DiskBinaryStore>(configuration, static (sp, config, name, optional) => {
            if (optional && !config.SectionExists("DiskBinaryStore", name))
                return null;
            
            var options = config.GetRequiredSection("DiskBinaryStore").GetRequiredSection<DiskBinaryStoreOptions>(name);
            var logger = sp.GetLogger<DiskBinaryStore>(name);
            return new(options, logger);
        });
    }
}