using System.Text.Json;

namespace CX.Engine.Common.JsonSchemas;

public class JsonSchemaOptions : IValidatable
{
    public JsonElement Schema { get; set; }
    public void Validate()
    {
    }
}