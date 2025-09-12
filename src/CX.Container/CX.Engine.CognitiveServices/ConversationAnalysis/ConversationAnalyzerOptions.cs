using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.ConversationAnalysis;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ConversationAnalyzerOptions : IValidatable
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public int MaxWaits { get; set; }
    public int CharacterLimit { get; set; }
    public TimeSpan WaitInterval { get; set; }
    public string ChatAgentName { get; set; }

    public string SummaryPrompt { get; set; }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class OutputFieldDetails
    {
        public string FieldType { get; set; }
        public List<string> Choices { get; set; }
    }

    public Dictionary<string, OutputFieldDetails> OutputFields { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChatAgentName))
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new ArgumentException($"{nameof(Endpoint)} is required");

            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException($"{nameof(ApiKey)} is required");

            if (MaxWaits < 1)
                throw new InvalidOperationException($"{nameof(MaxWaits)} must be greater than 0");

            if (WaitInterval < TimeSpan.FromMilliseconds(1))
                throw new InvalidOperationException($"{nameof(WaitInterval)} must be greater than 1 millisecond");

            if (CharacterLimit < 1)
                throw new InvalidOperationException($"{nameof(CharacterLimit)} must be greater than 0");
        }
        else
            if (string.IsNullOrWhiteSpace(SummaryPrompt))
                throw new ArgumentException($"{nameof(SummaryPrompt)} is required");
    }
}