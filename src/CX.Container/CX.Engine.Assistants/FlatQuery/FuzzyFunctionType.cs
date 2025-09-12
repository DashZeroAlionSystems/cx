namespace CX.Engine.Assistants.FlatQuery;

public enum FuzzyFunctionType
{
    //Fuzz.Process functions
    ExtractAll,
    ExtractSorted,
    ProcessFuncs,
    //Fuzz function
    PartialRatio,
    WeightedRatio,
    TokenSetRatio,
    PartialTokenSetRatio,
    Ratio,
    FuzzFunctions,
    //String function
    Contains,
    Equals,
    StringFunctions,
}