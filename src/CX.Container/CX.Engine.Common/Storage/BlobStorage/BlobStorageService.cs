using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CX.Engine.Common.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.Storage.BlobStorage;

public class BlobStorageService : IStorageService,
    ISnapshottedOptions<BlobStorageService.Snapshot, BlobStorageServiceOptions, BlobStorageService>
{
    private readonly string _name;
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;

    public BlobStorageService(MonitoredOptionsSection<BlobStorageServiceOptions> opts, ILogger logger,
        IServiceProvider sp, string name)
    {
        _logger = logger;
        _sp = sp;
        _name = name;
        opts.Bind<Snapshot, BlobStorageService>(this);
    }

    public class Snapshot : Snapshot<BlobStorageServiceOptions, BlobStorageService>,
        ISnapshotSyncInit<BlobStorageServiceOptions>
    {
        public BlobServiceClient BlobServiceClient;
        public BlobContainerClient BlobContainerClient;
        public PostgreSQLClient Client;

        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
            sp.GetRequiredNamedService(out Client, Options.PostgeSqlClientName, section);
            BlobServiceClient = new BlobServiceClient(Options.ConnectionString);
            BlobContainerClient = BlobServiceClient.GetBlobContainerClient(Options.ContainerName);
        }
    }

    public async Task<StorageResponseBase> GetContentAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetContentUrlAsync(string id)
    {
        var ss = CurrentShapshot;
        var item = await ss.Client.ListStringAsync(
            $"SELECT stored_value FROM {new InjectRaw(ss.Options.RelationName)} WHERE id = {new Guid(id)}");
        return item.FirstOrDefault();
    }

    public async Task<bool> Exists(string name)
    {
        var ss = CurrentShapshot;
        name = "%" + name + "%";
        var exists = await ss.Client.ExecuteAsync($"SELECT EXISTS (SELECT file_name FROM {new InjectRaw(ss.Options.RelationName)} WHERE file_name ILIKE {name})");
        if(exists is bool exist)
            return exist;
        return exists is int ? (int)exists == 1 : false;
    }
    
    // -----------------------------------------------------------------------------
    //  InsertContentAsync
    //  • allowDirectDownload  →  true  = return & persist SAS url
    //                          false = return & persist blob-id, GetContentAsync will serve it
    // -----------------------------------------------------------------------------
    public async Task<string> InsertContentAsync(string name, Stream content)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var ss = CurrentShapshot
                 ?? throw new InvalidOperationException("snapshot not ready");

        await ss.BlobContainerClient.CreateIfNotExistsAsync();

        var id = Guid.NewGuid();
        var blobName = $"{id:N}_{Path.GetFileName(name)}";
        var mime = Path.GetExtension(name).ToMimeType().GetContentType();

        // upload
        await ss.BlobContainerClient.GetBlobClient(blobName)
            .UploadAsync(content, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = mime });

        string storedValue; // what we record in Postgres
        // build 24-h SAS
        var bld = new BlobSasBuilder
        {
            BlobContainerName = ss.Options.ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
        };
        bld.SetPermissions(BlobSasPermissions.Read);

        var key = new Azure.Storage.StorageSharedKeyCredential(
            ss.Options.StorageAccountName, ss.Options.StorageAccountKey);
        storedValue = $"{ss.BlobContainerClient.Uri}/{blobName}?{bld.ToSasQueryParameters(key)}";

        await ss.Client.ExecuteAsync(
            $"INSERT INTO {new InjectRaw(ss.Options.RelationName)} (id, file_name, stored_value, mime_type) VALUES({id}, {name}, {storedValue}, {mime})");

        return id.ToString();
    }


    public Task DeleteContentAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<List<StorageResponseBase>> GetContentsAsync()
    {
        throw new NotImplementedException();
    }

    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<BlobStorageServiceOptions> OptionsSection { get; set; }
}