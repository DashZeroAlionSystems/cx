using Microsoft.Extensions.Configuration;

namespace CX.Engine.Common.Storage.FileStorage;

public class FileStorageServiceOptions : IValidatableConfiguration
{
    public string BasePath { get; set; }
    public string BaseUrl { get; set; }
    public void Validate(IConfigurationSection section)
    {
        section.ThrowIfNullOrWhiteSpace(BasePath);
        section.ThrowIfNullOrWhiteSpace(BaseUrl);
    }
}