using Microsoft.Extensions.Configuration;

namespace CX.Engine.Common.Storage.BlobStorage;

public class BlobStorageServiceOptions : IValidatableConfiguration
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public string PostgeSqlClientName { get; set; }
    public string RelationName { get; set; }
    public string StorageAccountName { get; set; }
    public string StorageAccountKey { get; set; }
    
    public void Validate(IConfigurationSection section)
    {
        section.ThrowIfNullOrWhiteSpace(ConnectionString);
        section.ThrowIfNullOrWhiteSpace(ContainerName);
        section.ThrowIfNullOrWhiteSpace(PostgeSqlClientName);
        section.ThrowIfNullOrWhiteSpace(RelationName);
        section.ThrowIfNullOrWhiteSpace(StorageAccountName);
        section.ThrowIfNullOrWhiteSpace(StorageAccountKey);
    }
}