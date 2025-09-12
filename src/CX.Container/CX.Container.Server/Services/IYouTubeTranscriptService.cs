namespace CX.Container.Server.Services;

public interface IYouTubeTranscriptService
{
    Task<byte[]> GetTranscriptAsync(string youtubeUrl);
}