using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.VectorMind;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class VectormindLiveAssistantOptions: IValidatable
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string TokenUrl { get; set; } = null!;
    
    public string APIBaseUrl { get; set; } = null!;
    public int MaxConcurrentAsks { get; set; }
    public int RetryDelayMs { get ; set; }
    
    //Set to the default uuid in bots table
    public string BotId { get; set; } = "00000000-0000-0000-0000-000000000000";

    public class StructuredOptions
    {
        public bool Enabled { get; set; }
        public string ChannelName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ChannelName))
                throw new ArgumentException($"{nameof(StructuredOptions)}.{nameof(ChannelName)} is required");
            
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException($"{nameof(StructuredOptions)}.{nameof(ApiKey)} is required");
            
            if (string.IsNullOrWhiteSpace(ApiSecret))
                throw new ArgumentException($"{nameof(StructuredOptions)}.{nameof(ApiSecret)} is required");
        }
    }

    public StructuredOptions Structured { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(APIBaseUrl))
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(APIBaseUrl)} is required");
        
        if (MaxConcurrentAsks < 1)
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(MaxConcurrentAsks)} must be greater than 0");
        
        if (RetryDelayMs < 1)
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(RetryDelayMs)} must be greater than or equal to 1ms");

        if (Structured?.Enabled ?? false)
        {
            Structured.Validate();   
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(ClientId)} is required");
        
        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(ClientSecret)} is required");
        
        if (string.IsNullOrWhiteSpace(Audience))
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(Audience)} is required");

        if (string.IsNullOrWhiteSpace(TokenUrl))
            throw new ArgumentException($"{nameof(VectormindLiveAssistantOptions)}.{nameof(TokenUrl)} is required");
    }
}