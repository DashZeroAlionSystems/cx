using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SmartFormat;

namespace CX.Engine.Assistants.TextToSchema;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TextToSchemaQuestion
{
    public bool Active { get; set; } = true;
    public string Prompt { get; set; }
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
    public string[] Choices { get; set; }
    public List<JsonElement> NullValues { get; set; }
    public bool IsArray { get; set; }
    public bool ShouldReason { get; set; } = true;

    public void Setup(IConfigurationSection section)
    {
        var sect = section.GetSection(nameof(NullValues));
        if (sect.Exists())
            NullValues = sect.IterateArray().Select(entry => entry.ToJsonElement()).ToList();
        else
            NullValues = null;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
            throw new InvalidOperationException($"{nameof(Prompt)} is required");

        if (string.IsNullOrWhiteSpace(PropertyName))
            throw new InvalidOperationException($"{nameof(PropertyName)} is required");

        if (string.IsNullOrWhiteSpace(PropertyType))
            throw new InvalidOperationException($"{nameof(PropertyType)} is required");
    }

    public void AddToJsonSchema(SchemaObject obj, Dictionary<string, string> parameters)
    {
        if (ShouldReason)
            obj.AddProperty(PropertyName + "reasoning", PrimitiveTypes.String, $"Reasoning for the {PropertyName} property");
        
        var choices = ExpandChoices(Choices, parameters);
        if (!IsArray)
            obj.AddProperty(PropertyName, PropertyType, Prompt, choices);
        else
            obj.AddProperty(PropertyName, PrimitiveTypes.Array, Prompt, choices, itemType: PropertyType);
    }

    public static List<string> ExpandChoices(string[] choices_arr, Dictionary<string, string> parameters)
    {
        if (choices_arr is null)
            return null;

        var choices = choices_arr.ToList();

        for (var i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            if (choice.Contains("{"))
            {
                parameters ??= new();
                var formatted = Smart.Format(choice, parameters);

                //expand comma-separated values
                if (formatted.Contains(","))
                {
                    //if a comma is escaped by a backslash, don't split at it
                    formatted = formatted.Replace("\\,", "{comma_121345}");
                    var split = formatted.Split(',');
                    for (var j = 0; j < split.Length; j++)
                        split[j] = split[j].Replace("{comma_121345}", ",");
                    choices.RemoveAt(i);
                    choices.InsertRange(i, split);
                    i--;
                }
                else
                    choices[i] = formatted;
            }
        }

        return choices;
    }

    public JsonObject GenerateJsonSchema(Dictionary<string, string> parameters)
    {
        var schema = new JsonObject
        {
            ["$schema"] = "http://json-schema.org/draft-07/schema#",
            ["type"] = "object",
            ["properties"] = new JsonObject(),
            ["required"] = new JsonArray(["reasoning", PropertyName]),
            ["additionalProperties"] = false
        };

        if (ShouldReason)
        {
            var property = new JsonObject();
            property["type"] = "string";
            ((JsonObject)schema["properties"])!["reasoning"] = property;
        }

        {
            var property = new JsonObject();

            if (IsArray)
            {
                // Define the property as an array type
                property["type"] = "array";
                var items = new JsonObject
                {
                    ["type"] = PropertyType
                };

                var choices = ExpandChoices(Choices, parameters);
                // Add choices to items if provided
                if (choices is { Count: > 0 })
                {
                    items["enum"] = new JsonArray(choices.Select(c => JsonValue.Create(c)).ToArray());
                }

                property["items"] = items;
            }
            else
            {
                // Define the property as a simple type
                property["type"] = PropertyType;

                var choices = ExpandChoices(Choices, parameters);
                // Add choices directly to the property if provided
                if (choices is { Count: > 0 })
                {
                    property["enum"] = new JsonArray(choices.Select(c => JsonValue.Create(c)).ToArray());
                }
            }

            // Add the property to the "properties" object
            ((JsonObject)schema["properties"])![PropertyName] = property;
        }

        return schema;
    }
}