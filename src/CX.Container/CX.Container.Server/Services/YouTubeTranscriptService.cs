using System.Text.RegularExpressions;
using YoutubeTranscriptApi;

namespace CX.Container.Server.Services;

public sealed class YouTubeTranscriptService : IYouTubeTranscriptService
{
    private readonly ILogger<YouTubeTranscriptService> _logger;
    private readonly HttpClient _httpClient;

    public YouTubeTranscriptService(ILogger<YouTubeTranscriptService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<byte[]> GetTranscriptAsync(string youtubeUrl)
    {
        try
        {
            var videoId = ExtractVideoIdFromUrl(youtubeUrl);
            if (string.IsNullOrEmpty(videoId))
            {
                throw new ArgumentException("Invalid YouTube URL");
            }

            var youTubeTranscriptApi = new YouTubeTranscriptApi();
            var transcriptItems = youTubeTranscriptApi.GetTranscript(videoId);

            if (transcriptItems == null || !transcriptItems.Any())
            {
                _logger.LogWarning("No transcript found for video: {VideoId}", videoId);
                throw new Exception("No transcript found for this video.");
            }

            // Filter out items that contain only "[Music]"
            var filteredTranscriptItems = transcriptItems
                .Where(item => !item.Text.Equals("[Music]", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Text.ToString());

            // Join transcript items with newline characters
            var combinedTranscript = string.Join(Environment.NewLine, filteredTranscriptItems);

            // Create a text file in memory
            await using var memoryStream = new MemoryStream();
            await using var writer = new StreamWriter(memoryStream);
            writer.Write(combinedTranscript);
            writer.Flush();
            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }

        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching the transcript.");
            throw new Exception("Network error occurred while fetching the transcript. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching the transcript.");
            throw new Exception(ex.Message);
        }
    }

    private static string ExtractVideoIdFromUrl(string url)
    {
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})");
        var match = regex.Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

}