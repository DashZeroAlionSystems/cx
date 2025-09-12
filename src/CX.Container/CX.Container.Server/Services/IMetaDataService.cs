using static CX.Container.Server.Services.OpenAiService;

namespace CX.Container.Server.Services;

public interface IMetadataService
{
    Task<ExtractedInformation> GetMetadataAsync(string extractedText, string displayName, string s3Key);
}