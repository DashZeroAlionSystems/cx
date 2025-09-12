using CX.Container.Server.Extensions.Application;
using static CX.Container.Server.Services.OpenAiService;

namespace CX.Container.Server.Services;

public class MetadataService : IMetadataService
{
    private readonly OpenAiService _pdfTextExtractorApiService;

    public MetadataService(OpenAiService pdfTextExtractorApiService)
    {
        _pdfTextExtractorApiService = pdfTextExtractorApiService;
    }

    public async Task<ExtractedInformation> GetMetadataAsync(string extractedText, string displayName, string s3Key)
    {
        extractedText = extractedText.Truncate(7900);
        var metaData = await _pdfTextExtractorApiService.SummarizeTextAsync(extractedText);
        metaData.Description = metaData.Description.Truncate(254);
        metaData.FileName = displayName;
        metaData.S3Key = s3Key;
        return metaData;
    }
}