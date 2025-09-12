using System.Dynamic;
using System.Reflection;
using System.Text;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using Cx.Engine.Common.PromptBuilders;
using JetBrains.Annotations;
using SmartFormat;

namespace CX.Engine.Assistants.ArtifactAssists;

[PublicAPI]
public class PromptActionsSection : PromptSection
{
    public string Header = "Actions (tools/methods) you can execute:";
    public readonly List<PromptAction> Actions = [];
    public string ChangeArtifactNotes;

    public bool HasActions => Actions?.Count > 0;

    public override string GetContent(ExpandoObject context)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var a in Actions)
        {
            var s = a.ShortSignature?.Trim();
            if (!string.IsNullOrWhiteSpace(a.UsageNotes))
                s += "\r\n" + a.UsageNotes.Trim();
            if (!string.IsNullOrWhiteSpace(s))
                sb.AppendLine($"- {s.Trim().NormalizeLineEndings().Indent(2, firstLine: false)}");
        }

        {
            var s = sb.ToString();

            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            return Smart.Format(s, context);
        }
    }

    public void Add(string action, string usageNotes = null) => Actions.Add(new(action, usageNotes));
    public void Add(PromptAction act) => Actions.Add(act);

    public void RemoveAllBoundActions() => Actions.RemoveAll(a => a.Action != null);

    /// <summary>
    /// Adds a signatured based on an AgentAction.
    /// </summary>
    /// <returns>If the action could be added .</returns>
    public bool TryAdd(AgentAction act)
    {
        if (act == null)
            return false;

        var shortSignature = act.GetCallSignature();

        var notesAttrs = act.Method?.Method.GetCustomAttributes(typeof(SemanticNoteAttribute));
        var sNotes = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(act.UsageNotes))
            sNotes.AppendLine(act.UsageNotes);

        if (notesAttrs != null)
            foreach (var note in notesAttrs)
            {
                if (note is SemanticNoteAttribute sNote)
                    if (!string.IsNullOrWhiteSpace(sNote.Note))
                        sNotes.AppendLine(sNote.Note);
            }

        var promptAction = new PromptAction(shortSignature, sNotes.ToString(), action: act);
        Add(promptAction);

        return true;
    }
}