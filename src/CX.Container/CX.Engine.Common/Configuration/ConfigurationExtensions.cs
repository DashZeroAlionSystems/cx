using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace CX.Engine.Common;

public static class ConfigurationExtensions
{
    public static JsonDocument ToJsonDocument(this IConfigurationSection configuration)
    {
        if (configuration == null)
            return null;

        var jsonElement = ToJsonElement(configuration);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        jsonElement.WriteTo(writer);
        writer.Flush();
        stream.Position = 0;
        return JsonDocument.Parse(stream);
    }

    public static List<IConfigurationSection> IterateArray(this IConfigurationSection section)
    {
        var list = new List<IConfigurationSection>();
        foreach (var child in section.GetChildren())
        {
            //only array indexes
            if (int.TryParse(child.Key, out _))
            {
                list.Add(child);
                list.AddRange(child.IterateArray());
            }
        }
        return list;
    }

    public static JsonElement? ToJsonElementNullable(this IConfigurationSection section)
    {
        if (section.Exists())
            return section.ToJsonElement();
        else
            return null;
    }

    public static JsonElement ToJsonElement(this IConfigurationSection section)
    {
        var children = section.GetChildren().ToList();

        // Leaf node
        if (!children.Any())
        {
            return section.Value switch
            {
                "True" => JsonSerializer.SerializeToElement(true),
                "False" => JsonSerializer.SerializeToElement(false),
                _ => 
                    int.TryParse(section.Value, out var val_i) ? JsonSerializer.SerializeToElement(val_i) :
                    long.TryParse(section.Value, out var val_l) ? JsonSerializer.SerializeToElement(val_l) :
                    double.TryParse(section.Value, out var val_d) ? JsonSerializer.SerializeToElement(val_d) :
                    JsonSerializer.SerializeToElement(section.Value)
            };
        }

        // Check if all keys are integers (array indices)
        var isArray = children.All(c => int.TryParse(c.Key, out _));

        if (isArray)
        {
            // Handle as array
            var elements = children.OrderBy(c => int.Parse(c.Key)).Select(ToJsonElement).ToList();
            return JsonSerializer.SerializeToElement(elements);
        }

        // Handle as object
        var properties = new Dictionary<string, JsonElement>();
        foreach (var child in children)
        {
            properties[child.Key] = ToJsonElement(child);
        }

        return JsonSerializer.SerializeToElement(properties);
    }
    
    public static JsonNode ToJsonNode(this IConfigurationSection section)
    {
        var children = section.GetChildren().ToList();

        // Leaf node
        if (!children.Any())
        {
            if (bool.TryParse(section.Value, out var boolValue))
            {
                return JsonValue.Create(boolValue);
            }

            if (int.TryParse(section.Value, out var intValue))
            {
                return JsonValue.Create(intValue);
            }

            if (double.TryParse(section.Value, out var doubleValue))
            {
                return JsonValue.Create(doubleValue);
            }

            if (string.IsNullOrEmpty(section.Value))
            {
                return null; // Represents JSON null
            }

            return JsonValue.Create(section.Value);
        }

        // Check if all keys are integers (array indices)
        var isArray = children.All(c => int.TryParse(c.Key, out _));

        if (isArray)
        {
            // Handle as array
            var array = new JsonArray();
            foreach (var child in children.OrderBy(c => int.Parse(c.Key)))
            {
                array.Add(child.ToJsonNode());
            }
            return array;
        }

        // Handle as object
        var obj = new JsonObject();
        foreach (var child in children)
        {
            obj[child.Key] = child.ToJsonNode();
        }
        return obj;
    }

    public static IConfigurationSection ForProperty(this IConfigurationSection section,
        object value, 
        [CallerArgumentExpression(nameof(value))] string propertyName = null)
    {
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));
        
        return section.GetSection(propertyName);
    }
}