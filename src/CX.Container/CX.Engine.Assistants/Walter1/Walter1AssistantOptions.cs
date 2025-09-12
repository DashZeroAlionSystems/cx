using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.Walter1;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Walter1AssistantOptions : IValidatable
{
    public double MinSimilarity { get; set; }
    public int CutoffContextTokens { get; set; }
    public int? MaxChunksPerAsk { get; set; }
    public int CutoffHistoryTokens { get; set; }
    
    public string ChatAgent { get; set; } = null!;
    public string DefaultSystemPrompt { get; set; } = null!;
    
    public string DefaultContextualizePrompt { get; set; }
    public string Archive { get; set; }
    public List<string> Archives { get; set; } 
    public string[] InputProcessors { get; set; }

    public int? TopDocumentLimit { get; set; }
    public bool SortChunks { get; set; } = true;
    public bool UseAttachments { get; set; } = true;
    
    public IEnumerable<string> EnumerateArchives()
    {
        if (!string.IsNullOrEmpty(Archive))
            yield return Archive;

        if (Archives != null)
            foreach (var archive in Archives)
                if (!string.IsNullOrEmpty(archive))
                    yield return archive;
    }

    public void Validate()
    {
        if (MinSimilarity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(MinSimilarity), "MinSimilarity must be between 0 and 1");
        
        if (CutoffContextTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(CutoffContextTokens), $"{nameof(CutoffContextTokens)} must be non-negative");
        
        if (CutoffHistoryTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(CutoffHistoryTokens), $"{nameof(CutoffHistoryTokens)} must be non-negative");
        
        if (MaxChunksPerAsk is < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxChunksPerAsk), $"{nameof(MaxChunksPerAsk)} must be null (unlimited) or greater than 0");

        if (string.IsNullOrEmpty(ChatAgent))
            throw new ArgumentException($"{nameof(Walter1AssistantOptions)}.{nameof(ChatAgent)} is required");
    }

    public Walter1AssistantOptions Clone()
    {
        return new()
        {
            MinSimilarity = MinSimilarity,
            CutoffContextTokens = CutoffContextTokens,
            MaxChunksPerAsk = MaxChunksPerAsk,
            CutoffHistoryTokens = CutoffHistoryTokens,
            ChatAgent = ChatAgent,
            DefaultSystemPrompt = DefaultSystemPrompt,
            DefaultContextualizePrompt = DefaultContextualizePrompt,
            Archive = Archive,
            Archives = Archives == null ? null : [..Archives],
            InputProcessors = InputProcessors,
            TopDocumentLimit = TopDocumentLimit,
            SortChunks = SortChunks,
            UseAttachments = UseAttachments
        };
    }
}