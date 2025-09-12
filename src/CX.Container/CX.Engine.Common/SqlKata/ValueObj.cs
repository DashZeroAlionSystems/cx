using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.Common.SqlKata;

public class ValueObj<T>
{
    [Semantic("Value(s) to be filtered on, set property to NONE or ['NONE'] when no filter is applied")]
    public T Value_s { get; set; }
}