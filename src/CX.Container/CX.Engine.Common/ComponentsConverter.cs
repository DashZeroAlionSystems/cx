using System.Text.Json;
using JsonDocument = System.Text.Json.JsonDocument;
using JsonElement = System.Text.Json.JsonElement;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using Utf8JsonReader = System.Text.Json.Utf8JsonReader;
using Utf8JsonWriter = System.Text.Json.Utf8JsonWriter;

namespace CX.Engine.Common;

public class ComponentsConverter<TComponent> : System.Text.Json.Serialization.JsonConverter<Components<TComponent>>
{
   public override Components<TComponent> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    // Expect a JSON array.
    if (reader.TokenType != JsonTokenType.StartArray)
    {
        throw new JsonException("Expected StartArray token.");
    }

    var components = new Components<TComponent>();

    // Read each element in the array.
    while (reader.Read())
    {
        // End of the array.
        if (reader.TokenType == JsonTokenType.EndArray)
            break;
            
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token for component.");

        // Parse the current component's JSON object.
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var element = doc.RootElement;

            // Ensure that the type discriminator is present.
            if (!element.TryGetProperty("$type", out var typeProperty))
            {
                throw new JsonException("Missing $type discriminator in component.");
            }

            var typeName = typeProperty.GetString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException("Invalid $type discriminator.");
            }

            // Obtain the actual type to be instantiated.
            var actualType = Type.GetType(typeName, throwOnError: true);
            
            if (actualType.IsAssignableTo(typeof(TComponent)) == false)
                throw new JsonException($"Type {actualType} is not assignable to {typeof(TComponent)}.");

            TComponent component;

            // If the serialized JSON includes a "$value" property,
            // assume the component was a primitive or array.
            if (element.TryGetProperty("$value", out var valueElement))
            {
                component = (TComponent)JsonSerializer.Deserialize(
                    valueElement.GetRawText(), actualType, options);
            }
            else
            {
                // For object-valued components, remove the "$type" property.
                var tempDict = new Dictionary<string, JsonElement>();
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("$type"))
                        continue;
                    tempDict.Add(property.Name, property.Value);
                }

                // Convert the remaining properties back into a JSON string.
                var tempJson = JsonSerializer.Serialize(tempDict);
                component = (TComponent)JsonSerializer.Deserialize(tempJson, actualType, options);
            }

            components.Add(component);
        }
    }

    return components;
}


    public override void Write(Utf8JsonWriter writer, Components<TComponent> value, JsonSerializerOptions options)
    {
        // Begin writing the array of components.
        writer.WriteStartArray();
    
        foreach (var component in value)
        {
            writer.WriteStartObject();
        
            // Write the type discriminator.
            writer.WriteString("$type", component.GetType().AssemblyQualifiedName);
        
            // Serialize the component into a temporary JSON string.
            var json = JsonSerializer.Serialize(component, component.GetType(), options);
            using (var doc = JsonDocument.Parse(json))
            {
                // If the serialized component is a JSON object,
                // merge its properties directly.
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        property.WriteTo(writer);
                    }
                }
                else
                {
                    // For arrays and primitives, wrap the raw value in a "$value" property.
                    writer.WritePropertyName("$value");
                    doc.RootElement.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
        }
    
        writer.WriteEndArray();
    }
}