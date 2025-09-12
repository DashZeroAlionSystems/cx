namespace CX.Engine.Common.Numbering;

public class AlphabeticOrderedListSequence : IOrderedListSequence
{
    public int CurrentPos { get; set; }
    public bool Uppercase = false;
    
    public string Current => Uppercase ? $"{(char)('A' + CurrentPos - 1)}" : $"{(char)('a' + CurrentPos - 1)}";
    
    public void Next() => CurrentPos++;
}