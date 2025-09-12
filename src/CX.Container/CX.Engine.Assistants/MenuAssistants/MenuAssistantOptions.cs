using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.MenuAssistants;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class MenuAssistantOptions : IValidatable
{
    public string Instructions { get; set; }
    public int InstructionsPriority { get; set; } = -1;
    public string ChatAgentName { get; set; }
    
    public List<MenuOption> Options { get; set; }
    public bool HasNoneOption { get; set; } = true;
    public string NoneOptionId { get; set; } = "None";
    public int NonePriority { get; set; } = 10_000;
    public bool EnableQuestionRephrase { get; set; } = true;
    public string NonePrompt { get; set; } = "If no appropriate option is listed, please select None as OptionID.  Users will only see your answer if None is selected - if you select an  Option it  will direct them to another agent that will answers their question.";

    public string DataStructurePrompt { get; set; } = 
        """
        Reply with a JSON packet containing two fields:
        - Answer (string): always provide an answer when OptionId is None.
        - AgentId (string): the agent (by id) to direct the user's question to.
        - Reasoning (string): Why this is the best option for the user's request.
        - QuestionToAskOtherAgent (string): The question to ask the agent selected by OptionId. 
        """;
    public int DataStructurePriority { get; set; }

    public bool EnableReasoning { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ChatAgentName))
            throw new InvalidOperationException($"{nameof(ChatAgentName)} is required.");
        
        if (string.IsNullOrWhiteSpace(Instructions))
            throw new InvalidOperationException($"{nameof(Instructions)} is required.");
        
        if (string.IsNullOrWhiteSpace(NoneOptionId))
            throw new InvalidOperationException($"{nameof(NoneOptionId)} is required.");
        
        if (string.IsNullOrWhiteSpace(NonePrompt))
            throw new InvalidOperationException($"{nameof(NonePrompt)} is required.");
        
        if (string.IsNullOrWhiteSpace(DataStructurePrompt))
            throw new InvalidOperationException($"{nameof(DataStructurePrompt)} is required.");

        if (Options == null)
            Options = [];

        foreach (var opt in Options)
            opt.Validate();
    }
}