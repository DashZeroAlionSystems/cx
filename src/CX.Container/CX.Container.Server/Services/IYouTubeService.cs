namespace CX.Container.Server.Services;

public interface IYouTubeService
{
    Task<byte[]> GetYouTubeAudioAs16kPcmBytesAsync(string youTubeUrl);
    Task<string> GetYouTubeVideoTitleAsync(string youTubeUrl);    
}