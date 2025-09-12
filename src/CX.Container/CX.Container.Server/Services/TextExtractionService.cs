using System.Text;
using CX.Engine.DocExtractors.Text;

namespace CX.Container.Server.Services;

public class TextExtractionService : ITextExtractionService
{
    private readonly PDFPlumber _pdfService;
    private readonly IYouTubeTranscriptService _youTubeTranscriptService;
    private readonly HttpClient _httpClient;

    public TextExtractionService(PDFPlumber pdfService, IYouTubeTranscriptService youTubeTranscriptService, HttpClient httpClient)
    {
        _pdfService = pdfService;
        _youTubeTranscriptService = youTubeTranscriptService;
        _httpClient = httpClient;
    }

    public async Task<string> ExtractTextFromYouTubeAsync(string url)
    {
        try
        {
			var textFileBytes = await _youTubeTranscriptService.GetTranscriptAsync(url);
			return Encoding.UTF8.GetString(textFileBytes);
		}
        catch
        {
            return null;
        }
    }

    public async Task<string> ExtractTextFromPdfAsync(string presignedUrl) // New method implementation
    {
        var response = await _httpClient.GetAsync(presignedUrl);
        response.EnsureSuccessStatusCode();

        using (var pdfStream = await response.Content.ReadAsStreamAsync())
        {
            return await _pdfService.ExtractToTextAsync(pdfStream, new());
        }
    }
}