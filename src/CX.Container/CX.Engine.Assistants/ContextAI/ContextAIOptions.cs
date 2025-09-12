using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.ContextAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ContextAIOptions : IValidatable
{
    public string Host { get; set; } = "https://api.context.ai";
    public string ConversationThreadEndpoint { get; set; } = "/api/v1/log/conversation/thread";
    public string ConversationThreadEndpointFull => Host + ConversationThreadEndpoint;
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = null!;
    public string TenantId { get; set; }

    public void Validate()
    {
        if (Enabled && string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException($"{nameof(ContextAIOptions)}.{nameof(ApiKey)} is required");

        if (!string.IsNullOrWhiteSpace(TenantId))
        {
            if (TenantId.Length is < 1 or > 40)
                throw new InvalidOperationException(
                    $"{nameof(ContextAIOptions)}.{nameof(TenantId)} must be between 1 and 40 characters long");
        }
        
        if (string.IsNullOrWhiteSpace(ConversationThreadEndpoint))
            throw new InvalidOperationException($"{nameof(ContextAIOptions)}.{nameof(ConversationThreadEndpoint)} is required");
    }
}