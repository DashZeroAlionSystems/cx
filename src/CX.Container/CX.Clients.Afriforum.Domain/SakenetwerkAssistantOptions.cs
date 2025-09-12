using JetBrains.Annotations;

namespace CX.Clients.Afriforum.Domain;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SakenetwerkAssistantOptions
{
   public bool AdminMode { get; set; }
   public string JsonStoreName { get; set; } = null!;
   public string OpenAIAgentName { get; set; } = null!;
   public string CleanCitiesPrompt { get; set; } = null!;
   public string CleanProvincesPrompt { get; set; } = null!;
   public string ExpandPrompt { get; set; } = null!;
   
   public string[] ContextualizePromptLines { get; set; } = null!;
   public string ContextualizePrompt { get; set; } = null!;
   public string[] SystemPromptLines { get; set; } = null!;
   public string SystemPrompt { get; set; } = null!;
   
   public string PostgreSQLClientName { get; set; } = null!;

   public void Validate()
   {
      if (string.IsNullOrWhiteSpace(JsonStoreName))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(JsonStoreName)} is required");
      
      if (string.IsNullOrWhiteSpace(OpenAIAgentName))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(OpenAIAgentName)} is required");
      
      if (string.IsNullOrWhiteSpace(CleanCitiesPrompt))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(CleanCitiesPrompt)} is required");
      
      if (string.IsNullOrWhiteSpace(ExpandPrompt))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(ExpandPrompt)} is required");
      
      if (string.IsNullOrWhiteSpace(CleanProvincesPrompt))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(CleanProvincesPrompt)} is required");
      
      if (ContextualizePromptLines != null)
         ContextualizePrompt += ("\n" + string.Join("\n", ContextualizePromptLines)).Trim();
      
      if (string.IsNullOrWhiteSpace(ContextualizePrompt))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(ContextualizePrompt)} is required");
      
      if (SystemPromptLines != null)
          SystemPrompt += ("\n" + string.Join("\n", SystemPromptLines)).Trim();
      
      if (string.IsNullOrWhiteSpace(SystemPrompt))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(SystemPrompt)} is required");
      
      if (string.IsNullOrWhiteSpace(PostgreSQLClientName))
         throw new InvalidOperationException($"{nameof(SakenetwerkAssistantOptions)}.{nameof(PostgreSQLClientName)} is required");
   }
}