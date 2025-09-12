using Microsoft.Extensions.Configuration;

namespace CX.Engine.Common.Storage.PostgreSQLFileStorage;

public class PostgreSQLStorageServiceOptions : IValidatableConfiguration
{
    public string PostgeSqlClientName { get; set; }
    public string RelationName { get; set; }
    public string BaseUrl { get; set; }
    public void Validate(IConfigurationSection section)
    {
        section.ThrowIfNullOrWhiteSpace(BaseUrl);
        section.ThrowIfNullOrWhiteSpace(PostgeSqlClientName);
        section.ThrowIfNullOrWhiteSpace(RelationName);
    }
}