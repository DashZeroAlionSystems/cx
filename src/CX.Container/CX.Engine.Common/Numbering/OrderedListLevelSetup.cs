namespace CX.Engine.Common.Numbering;

public class OrderedListLevelSetup
{
    public OrderedListSequenceType SequenceType;
    public string Delimiter;

    public OrderedListLevelSetup()
    {
        SequenceType = OrderedListSequenceType.Numeric;
        Delimiter = ".";
    }

    public OrderedListLevelSetup(OrderedListSequenceType sequenceType, string delimiter)
    {
        SequenceType = sequenceType;
        Delimiter = delimiter;
    }
    
    public static implicit operator OrderedListLevelSetup((OrderedListSequenceType sequenceType, string delimiter) setup) =>
        new OrderedListLevelSetup(setup.sequenceType, setup.delimiter);
}