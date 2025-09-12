using JetBrains.Annotations;

namespace CX.Engine.Assistants.FlatQuery;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class QueryFilterField
{
    public FlatQueryFilterFieldType DataType { get; set; }
    public string SemanticValuesQuery { get; set; }
    public bool? SemanticValuesAny { get; set; }
    public string FieldName { get; set; } = null!;
    public bool AllowFuzzyMatching { get; set; }
    public bool AllowMultiple { get; set; }
    public string FieldRules { get; set; }
    public string SortRules { get; set; }
    public bool AllowSort { get; set; }
    public int? IntNotSpecifiedValue { get; set; } = -1;
    public int FuzzyWeight { get; set; } = 1;
    public FuzzyFunctionType FuzzyFunction { get; set; } = FuzzyFunctionType.ExtractAll;

    public void Validate()
    {
        if (DataType == FlatQueryFilterFieldType.None)
            throw new InvalidOperationException($"{nameof(QueryFilterField)}.{nameof(DataType)} is required");
        
        if (string.IsNullOrWhiteSpace(FieldName))
            throw new InvalidOperationException($"{nameof(QueryFilterField)}.{nameof(FieldName)} is required");
        
        if (DataType is not (FlatQueryFilterFieldType.String or FlatQueryFilterFieldType.Array))
        {
            if (AllowMultiple)
                throw new InvalidOperationException($"{FieldName} {nameof(QueryFilterField)}.{nameof(AllowMultiple)} can only be true for string data types");
            
            if (!string.IsNullOrWhiteSpace(SemanticValuesQuery))
                throw new InvalidOperationException($"{FieldName} {nameof(QueryFilterField)}.{nameof(SemanticValuesQuery)} can only be set for string data types");
        }
    }

    public QueryFilterField Clone() =>
        new()
        {
            DataType = DataType,
            SemanticValuesQuery = SemanticValuesQuery,
            SemanticValuesAny = SemanticValuesAny,
            FieldName = FieldName,
            AllowMultiple = AllowMultiple,
            FieldRules = FieldRules,
            IntNotSpecifiedValue = IntNotSpecifiedValue,
            AllowSort = AllowSort,
            SortRules = SortRules,
            AllowFuzzyMatching = AllowFuzzyMatching,
            FuzzyWeight = FuzzyWeight,
            FuzzyFunction = FuzzyFunction
        };
}