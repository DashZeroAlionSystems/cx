using System.Diagnostics.CodeAnalysis;
using CX.Engine.CognitiveServices.Transcriptions;

namespace CX.Engine.CognitiveServices.VoiceTranscripts;

public class TranscriptionApiException : TranscriptionException
{
    public readonly string ErrorCode;
    public string ErrorMessage;

    public TranscriptionApiException([NotNull] string errorCode, [NotNull] string errorMessage) : base($"{errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }
}