using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.Common.SqlKata;

public class MinMaxObj<T>
{
    [Semantic("Minimum value to filter")]
    public T Min { get; set; }
    [Semantic("Maximum value to filter")]
    public T Max { get; set; }
}