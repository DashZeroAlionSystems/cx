namespace CX.Container.Server.Services;

public interface ITextExtractionService
{    
    Task<string> ExtractTextFromYouTubeAsync(string url);
    Task<string> ExtractTextFromPdfAsync(string presignedUrl);
}