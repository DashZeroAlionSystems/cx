using CX.Engine.Common.Storage.BlobStorage;
using CX.Engine.Common.Storage.PostgreSQLFileStorage;
using CX.Engine.Common.Storage.FileStorage;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Storage;

public static class StorageServiceDI
{
    public const string PgStorageEngineName = "pg-storage";
    public const string StorageEngineName = "file-storage";
    public const string BlobStorageEngineName = "blob-storage";
    public static void AddStorageServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddPostgreSQLStorageServices(configuration);
        sc.AddFileStorageService(configuration);
        sc.AddBlobStorageServices(configuration);

        var router = sc.AddNamedTransientRouter<IStorageService>(configuration, "storage services");
        router[PgStorageEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<PostgreSQLStorageService>(subName, optional);
        router[StorageEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<FileStorageService>(subName, optional);
        router[BlobStorageEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<BlobStorageService>(subName, optional);
    }
}