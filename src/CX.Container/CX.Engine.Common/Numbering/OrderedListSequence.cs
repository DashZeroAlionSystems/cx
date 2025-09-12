namespace CX.Engine.Common.Numbering;

public interface IOrderedListSequence
{
    public int CurrentPos { get; set;  }
    public string Current { get; }

    public void Next();
}