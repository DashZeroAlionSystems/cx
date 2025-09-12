namespace CX.Container.Server.Services;

public interface IMicrosoftCognitiveSpeechService
{    
    Task<string> RecognizeSpeechFromFileAsync(string audioFilePath, string language);
    Task<string> RecognizeSpeechFromByteArrayAsync(byte[] byteArray, string language);
}