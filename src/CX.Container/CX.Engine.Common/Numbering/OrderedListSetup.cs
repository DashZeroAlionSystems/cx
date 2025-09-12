namespace CX.Engine.Common.Numbering;

public class OrderedListSetup
{
    public List<OrderedListLevelSetup> Levels = [];
    public OrderedListSetup Parent;
    public string Prefix;
    public string Delimiter = ".";
    public string Suffix = " ";
    public int Level = 1;
    public IOrderedListSequence Sequence;
    
    public string CurrentWithoutSuffix => Prefix + Sequence.Current + Delimiter;
    public string Current => CurrentWithoutSuffix + Suffix;

    public OrderedListSetup()
    {
        Setup(1);
    }
    
    public OrderedListSetup(params OrderedListLevelSetup[] levels)
    {
        Levels.AddRange(levels);
        Setup(1);
    }

    public OrderedListLevelSetup GetSetupForLevel(int level)
    {
        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level), "Level must be greater than 0");

        if (level <= Levels.Count)
            return Levels[level - 1];

        return new();
    }

    public string Next() 
    {
        Sequence.Next();
        return Current;
    }

    public OrderedListSetup Nest() =>
        new OrderedListSetup()
        {
            Parent = this,
            Prefix = CurrentWithoutSuffix,
            Suffix = Suffix,
            Level = Level + 1,
            Levels = Levels
        }.Setup(Level + 1);

    public OrderedListSetup Setup(OrderedListLevelSetup levelSetup)
    {
        Sequence = levelSetup.SequenceType.NewSequence();
        Delimiter = levelSetup.Delimiter;
        return this;
    }

    public OrderedListSetup Setup(int level)
    {
        var levelSetup = GetSetupForLevel(level);
        return Setup(levelSetup);
    }
}