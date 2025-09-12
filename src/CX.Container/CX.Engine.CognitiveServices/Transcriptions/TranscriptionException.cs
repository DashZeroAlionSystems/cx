using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.Transcriptions;

public abstract class TranscriptionException : Exception
{
    protected TranscriptionException()
    {
    }

    protected TranscriptionException([CanBeNull] string message) : base(message)
    {
    }

    protected TranscriptionException([CanBeNull] string message, [CanBeNull] Exception innerException) : base(message, innerException)
    {
    }
}