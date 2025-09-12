using System.Text.Json;
using JetBrains.Annotations;

namespace CX.Engine.Common.JsonSchemas;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class TypeDefinition
{
    public string Type { get; set; }
    public string Description { get; set; }
    public string ItemType { get; set; }
    public List<string> Choices { get; set; }
    public SchemaObject Object { get; set; }
    public TypeDefinition[] AnyOf { get; set; }
    public bool NullableType { get; set; }

    public TypeDefinition()
    {}
    
    public TypeDefinition(string type, string description = null, List<string> choices = null, string itemType = null, SchemaObject obj = null, TypeDefinition[] anyOf = null, bool nullable = false)
    {
        Choices = choices ?? new();
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Description = description;
        ItemType = itemType;
        Object = obj;
        AnyOf = anyOf;
        NullableType = nullable;
    }
    
    public override int GetHashCode() => throw new NotSupportedException();

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;
        
        if (obj is not TypeDefinition other)
            return false;
        
        // Compare Type (required field)
        if (!string.Equals(Type, other.Type, StringComparison.Ordinal))
            return false;
        
        // Compare optional fields
        var descriptionsEqual = (string.IsNullOrEmpty(Description) && string.IsNullOrEmpty(other.Description)) ||
                                string.Equals(Description, other.Description, StringComparison.Ordinal);
                           
        var itemTypesEqual = (string.IsNullOrEmpty(ItemType) && string.IsNullOrEmpty(other.ItemType)) ||
                             string.Equals(ItemType, other.ItemType, StringComparison.Ordinal);
                         
        var choicesEqual = (Choices == null || !Choices.Any()) && (other.Choices == null || !other.Choices.Any()) ||
                           (Choices != null && other.Choices != null && 
                            Choices.Count == other.Choices.Count && 
                            Choices.OrderBy(x => x).SequenceEqual(other.Choices.OrderBy(x => x)));
                        
        var objectsEqual = (Object == null && other.Object == null) ||
                           (Object?.Equals(other.Object) == true);
                      
        var anyOfEqual = (AnyOf == null || AnyOf.Length == 0) && (other.AnyOf == null || other.AnyOf.Length == 0) ||
                         (AnyOf != null && other.AnyOf != null && 
                          AnyOf.Length == other.AnyOf.Length &&
                          AnyOf.OrderBy(x => x.Type).SequenceEqual(other.AnyOf.OrderBy(x => x.Type)));

        return descriptionsEqual && 
               itemTypesEqual && 
               choicesEqual && 
               objectsEqual && 
               anyOfEqual;
    }

    public TypeDefinition(SchemaObject obj)
    {
        Type = "object";
        Object = obj;
    }

    public void Serialize(Utf8JsonWriter jw, bool gemini = false)
    {
        if (Object != null && Type != "array" && !(AnyOf?.Length > 0))
        {
            Object.Serialize(jw, isNullable: NullableType);
            return;
        }

        jw.WriteStartObject();

        if (AnyOf?.Length > 0)
        {
            jw.WritePropertyName("anyOf"u8);
            jw.WriteStartArray();

            foreach (var item in AnyOf)
            {
                item.Serialize(jw);
            }

            jw.WriteEndArray();
        }

        if (Type != null)
            if (NullableType)
            {
                jw.WriteStartArray("type"u8);
                jw.WriteStringValue(Type);
                jw.WriteStringValue("null");
                jw.WriteEndArray();
            }
            else
                jw.WriteString("type"u8, Type);

        if (Description != null)
            jw.WriteString("description"u8, Description);

        if (Choices?.Count > 0 && (ItemType == null || (gemini && Type != "array")))
        {
            jw.WritePropertyName("enum"u8);
            jw.WriteStartArray();

            foreach (var choice in Choices)
                jw.WriteStringValue(choice);

            jw.WriteEndArray();
        }

        if (ItemType != null && (!gemini || Type == "array"))
        {
            jw.WriteStartObject("items"u8);

            if (Object != null)
            {
                Object.Serialize(jw, writeStartObject: false);
                jw.WriteEndObject();
                return;
            }

            jw.WriteString("type"u8, ItemType);

            if (Choices?.Count > 0)
            {
                jw.WritePropertyName("enum"u8);
                jw.WriteStartArray();

                foreach (var choice in Choices)
                    jw.WriteStringValue(choice);

                jw.WriteEndArray();
            }
            
            jw.WriteEndObject();
        }

        jw.WriteEndObject();
    }
}