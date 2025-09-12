using CX.Engine.Common;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CX.Engine.DemoConsole;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MoonyConsoleServiceOptions
{
    public string QAFilePath { get; set; }
    public string GoogleSheetId { get; set; }
    public string GoogleSheetsApiKey { get; set; }
    public string AssistantChannelName { get; set; } = null!;
    public string ProdAssistantChannelNameOverwrite { get; set; } = null!;
    
    [JsonIgnore]
    public string ProdAssistantChannelName => ProdAssistantChannelNameOverwrite.NullIfWhiteSpace() ?? AssistantChannelName;
    public string AutoQA { get; set; }
    public string UploadArchive { get; set; } = null!;
    public string LuaCore { get; set; } = "lua_default";
    public bool WaitForQuestionCompletion { get; set; } = true;
    public bool NoMemory { get; set; }
    public bool GenerateQuizEvals { get; set; }
    public bool UseFileService { get; set; }
    public bool? CatchCommandExceptions { get; set; }
    public bool? CatchAskExceptions { get; set; }
    public string NoAnswer { get; set; } = "<No answer>";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AssistantChannelName))
            throw new InvalidOperationException($"{nameof(MoonyConsoleServiceOptions)}.{nameof(AssistantChannelName)} is required.");
        
        if (string.IsNullOrWhiteSpace(UploadArchive))
            throw new InvalidOperationException($"{nameof(MoonyConsoleServiceOptions)}.{nameof(UploadArchive)} is required.");
    }
}