using System.Text.Json;
using CX.Engine.HelmTemplates.Yaml;

namespace CX.Engine.HelmTemplates;

public static class Utf8JsonWriterExtensions
{
    public static void WriteYamlRef(this Utf8JsonWriter jw, string property, YamlValue value)
    {
        jw.WritePropertyName(property);
        jw.WriteGoValue(value.Exp());
    }
    
    public static void WriteYamlRefValue(this Utf8JsonWriter jw, YamlValue value)
    {
        jw.WriteGoValue(value.Exp());
    }

    public static void WriteGo(this Utf8JsonWriter jw, string property, GoExpression exp)
    {
        jw.WritePropertyName(property);
        jw.WriteGoValue(exp);
    }

    public static void WriteGoValue(this Utf8JsonWriter jw, GoExpression exp)
    {
        jw.WriteRawValue(exp.Emit(), true);
    }
}