namespace CX.Engine.Common.JsonSchemas;

public static class PrimitiveTypes
{
    public const string String = "string";
    public const string Array = "array";
    public const string Object = "object";
    public const string Boolean = "boolean";
    public const string Integer = "integer";
    public const string Number = "number";
    
    public static bool IsValid(string type) => type is String or Array or Object or Boolean or Integer or Number; 
}