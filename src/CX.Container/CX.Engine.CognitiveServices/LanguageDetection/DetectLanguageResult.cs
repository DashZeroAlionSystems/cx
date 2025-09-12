using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.LanguageDetection;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record DetectLanguageResult(string Language, string IsoCode, double Confidence);