using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CX.Engine.ChatAgents;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Tracing.Langfuse;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Assistants.ArtifactAssists;

public class ArtifactAssistRequest
{
    public string AgentName;
    public string LoggerName;
    public IChatAgent Agent;
    public readonly List<ChatMessage> History = new();
    public string Question;
    public string StringArtifact;
    public readonly ArtifactAssistPromptBuilder Prompt = new();
    public string CurrentArtifactDescriptionPrompt;
    public string ReasoningEffort;
    public LangfuseService LangfuseService;
    public ILogger Logger;
    public bool UpdateArtifactInRequest = true;
    public bool AddArtifactToChatHistoryOnError = false;
    public bool AddArtifactDiffToChatHistoryOnError = false;
    public bool AddArtifactChangeMessage = false;
    public bool AddChangeArtifactAction = true;
    public bool DebugMode = false;
    public bool AddNoAction = true;
    public SchemaObject SchemaObject;
    public bool UseExecutionPlan = false;
    public Func<string, Task> OnStringArtifactChangedAsync;
    public Action<ChatMessage> OnAddedToHistory;
    public int ArtifactExceptionsAllowed = 10;
    public int ActionsAllowed = 10;
    public string SchemaName = "artifact_assist";
    public string ChangeArtifactMethodName = "change_current_artifact";
    public string ChangeArtifactPropertyName = "changed_artifact";
    public string ChangeArtifactKeyPropertyName;
    public string ChangeArtifactKeyPropertyType = PrimitiveTypes.String;
    public Action<JsonElement> OnChangeArtifactValidateKey; 
    public readonly List<AgentAction> Actions = [];
    public Func<string, string> OnCleanArtifact;
    public Action<AgentAction, JsonDocument> OnStartAction;
    public Action<SchemaBase> AfterSchemaBuilt;
    public bool AllowChaining = true;
    public bool AllowDuplicateExceptions = true;
    public string AllowDuplicateExceptionsMessage = string.Empty;
    public readonly List<string> FailedAttempts = [];
    public Action<bool> OnDuplicateException;
    public bool OneShot = false;

    public void Validate()
    {
        if (Agent == null && string.IsNullOrWhiteSpace(AgentName))
            throw new ValidationException($"Either {nameof(Agent)} or {nameof(AgentName)} must be set.");
        
        if (string.IsNullOrWhiteSpace(SchemaName))
            throw new ValidationException($"{nameof(SchemaName)} must be set.");
        
        if (string.IsNullOrWhiteSpace(ChangeArtifactMethodName))
            throw new ValidationException($"{nameof(ChangeArtifactMethodName)} must be set.");
        
        if (string.IsNullOrWhiteSpace(ChangeArtifactPropertyName))
            throw new ValidationException($"{nameof(ChangeArtifactPropertyName)} must be set.");
    }
}

public class ArtifactAssistRequest<T> : ArtifactAssistRequest
{
    private T _artifact;
    public T Artifact
    {
        get => _artifact;
        set
        {
            _artifact = value;
            StringArtifact = JsonSerializer.Serialize(value, ArtifactAssist.ArtifactSerializerOptions);
        }
    }
    public Func<T, Task> OnArtifactChangedAsync;
    public Action<T> OnValidate;
    public Func<T, Task> OnValidateAsync;
    
    public ArtifactAssistRequest(T value, string question)
    {
        StringArtifact = JsonSerializer.Serialize(value, ArtifactAssist.ArtifactSerializerOptions);
        LoggerName = nameof(T);
        SchemaObject = new();
        SchemaObject.AddPropertiesFrom(typeof(T));
        SchemaName = $"artifact_assist_{nameof(T)}";
        Question = question;
        OnCleanArtifact = s => {
            try
            {
                var changed_artifact_json = JsonSerializer.Deserialize<T>(s);
                return JsonSerializer.Serialize(changed_artifact_json, ArtifactAssist.ArtifactSerializerOptions);
            }
            catch (Exception ex)
            {
                throw new ArtifactException($"Cleaning JSON artifact: {ex.Message}");
            }
        };
        OnStringArtifactChangedAsync = async newValue =>
        {
            T artifact;
            try
            {
                artifact = JsonSerializer.Deserialize<T>(newValue);
            }
            catch (Exception ex)
            {
                throw new ArtifactException($"Exception deserializing JSON artifact: {ex.Message}");
            }

            if (OnValidate != null)
            {
                try
                {
                    OnValidate(artifact);
                }
                catch (Exception ex)
                {
                    throw new ArtifactException($"Validating: {ex.Message}");
                }
            }
            
            if (OnValidateAsync != null)
            {
                try
                {
                    await OnValidateAsync(artifact);
                }
                catch (Exception ex)
                {
                    throw new ArtifactException($"Validating: {ex.Message}");
                }
            }

            Artifact = artifact;

            if (OnArtifactChangedAsync != null)
                await OnArtifactChangedAsync(Artifact);
        };
    }
}