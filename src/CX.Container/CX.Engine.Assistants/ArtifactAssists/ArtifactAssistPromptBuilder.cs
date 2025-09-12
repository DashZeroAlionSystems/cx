using System.Text;
using Cx.Engine.Common.PromptBuilders;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.ArtifactAssists;

[PublicAPI]
public class ArtifactAssistPromptBuilder : PromptBuilder
{
    public PromptContentSection Instructions;
    public PromptContentSection ArtifactAssistInstructions;
    public PromptActionsSection Actions;
    public PromptContentSection ResponseFormat;
    public PromptContentSection CurrentState;

    public ArtifactAssistPromptBuilder()
    {
        Instructions = Add("");
        ArtifactAssistInstructions = Add(
            "Respond to the user and/or determine the next action that you should take to complete the user's request.  Consider data needed for decisions before and actions already taken from the history.  Don't take any actions not specifically requested by the user." +
            $"Determine the correct actions to execute to complete the user's request (do nothing more than what was specifically requested) and output the next action, if any, into the {ArtifactAssist.Property_Action} property of your JSON response.");
        Actions = Add(new PromptActionsSection());
        CurrentState = Add(new PromptContentSection("", 10_000));
        ResponseFormat = Add(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("Response JSON data structure:");
            sb.AppendLine($"  {ArtifactAssist.Property_ExecutionPlan}: explain the next steps needed to complete the user's request, and why, in natural language and pseudo-code with methods and arguments detailed.");
            sb.AppendLine($"  {ArtifactAssist.Property_QuestionResponse}: a natural language response to the user's request.");
            
            if (Actions.HasActions)
                sb.AppendLine($"  {ArtifactAssist.Property_Action}: the next action to take to complete remaining parts of the user's request.");
            
            return sb.ToString();
        });
    }
}