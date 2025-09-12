namespace CX.Engine.CognitiveServices.Transcriptions;

public class TranscriptionInvalidFileFormatException : TranscriptionException
{
    public TranscriptionInvalidFileFormatException(string message) : base(message)
    {
    }
}