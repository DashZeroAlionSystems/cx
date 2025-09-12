using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.Transcriptions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record TranscriptionPhrase(int Speaker, string Phrase, TimeSpan? Offset = null, TimeSpan? Duration = null);
