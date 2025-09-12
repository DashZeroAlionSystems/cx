namespace CX.Engine.Common.Numbering;

public enum OrderedListSequenceType
{
    Unknown = 0,
    Alphabetic = 1,
    Numeric = 2,
    Roman = 3
}

public static class OrderedListSequenceTypeExt
{
    public static IOrderedListSequence NewSequence(this OrderedListSequenceType sequenceType)
    {
        return sequenceType switch
        {
            OrderedListSequenceType.Alphabetic => new AlphabeticOrderedListSequence(),
            OrderedListSequenceType.Numeric => new NumericOrderedListSequence(),
            OrderedListSequenceType.Roman => new RomanNumeralOrderedListSequence(),
            _ => throw new ArgumentOutOfRangeException(nameof(sequenceType), sequenceType, null)
        };
    }
}