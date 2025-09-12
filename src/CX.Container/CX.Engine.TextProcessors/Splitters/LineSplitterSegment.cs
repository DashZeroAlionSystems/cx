namespace CX.Engine.TextProcessors.Splitters;

public class LineSplitterSegment
{
    public virtual bool CanMergeWith(LineSplitterSegment next, int tokenLimit, int stage)
    {
        return false;
    }
    
    public virtual LineSplitterSegment Merge(LineSplitterSegment next)
    {
        throw new NotSupportedException();
    }
}