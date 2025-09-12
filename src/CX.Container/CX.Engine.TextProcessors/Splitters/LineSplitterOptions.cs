using JetBrains.Annotations;

namespace CX.Engine.TextProcessors.Splitters;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class LineSplitterOptions
{
    public int SegmentTokenLimit { get; set; }

    public void Validate()
    {
        if (SegmentTokenLimit <= 0)
            throw new ArgumentException("SegmentTokenLimit must be greater than 0");
    }
}