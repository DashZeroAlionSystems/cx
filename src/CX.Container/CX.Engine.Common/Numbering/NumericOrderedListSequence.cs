namespace CX.Engine.Common.Numbering;

public class NumericOrderedListSequence : IOrderedListSequence
{
    public int CurrentPos { get; set; }
    
    public string Current => $"{CurrentPos}";

    public void Next() => CurrentPos++;
}