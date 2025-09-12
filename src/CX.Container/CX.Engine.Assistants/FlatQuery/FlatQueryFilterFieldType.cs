using System.Runtime.Serialization;
using System.Text.Json.Serialization;
 
namespace CX.Engine.Assistants.FlatQuery;
 
[JsonConverter(typeof(JsonStringEnumConverter))]    // Tells Newtonsoft.Json to read/write this enum by string
public enum FlatQueryFilterFieldType
{
    [EnumMember(Value = "None")]
    None,
 
    [EnumMember(Value = "String")]
    String,
 
    [EnumMember(Value = "Integer")]
    Integer,
 
    [EnumMember(Value = "Double")]
    Double,
 
    [EnumMember(Value = "Array")]
    Array
}