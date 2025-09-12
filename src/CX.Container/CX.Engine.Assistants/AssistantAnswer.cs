using System.Text.Json.Serialization;
using CX.Engine.Assistants.Walter1;
using CX.Engine.Common;
using CX.Engine.TextProcessors;

namespace CX.Engine.Assistants;

public class AssistantAnswer
{
    public TextValidationException TextValidationException;
    [JsonInclude]
    public string Answer;
    [JsonInclude]
    public List<AttachmentInfo> Attachments;
    public readonly List<RankedChunk> Chunks = [];
    public string EmbeddingLookup;
    public readonly List<AttachmentInfo> InputAttachments = [];
    public string SystemPrompt;
    [JsonInclude]
    public bool IsRefusal;

    public List<KeyValuePair<string, Walter1Helpers.TopKScore>> DocumentFilter; 

    public string GetCellContent()
    {
        if (Attachments != null)
            return (Answer + "\n\n" + string.Join("\n", Attachments.Select(att => "Attachment: " + att.FileName + "(" + att.FileUrl + ")"))).Trim();

        return Answer;
    }

    public AssistantAnswer()
    {
    }

    public AssistantAnswer(string answer)
    {
        Answer = answer;
    }

    public override string ToString()
    {
        return Answer;
    }
}