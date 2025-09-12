using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Json.More;

namespace CX.Engine.Common.JsonSchemas;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SchemaObject
{
    public Dictionary<string, TypeDefinition> Properties { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
    public SchemaObject AdditionalProperties { get; set; }

    public SchemaObject()
    {
    }

    public override int GetHashCode() => throw new NotSupportedException();

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;
        
        if (obj is not SchemaObject other)
            return false;
    
        // If both dictionaries are null or empty, they're equal
        if ((Properties == null || !Properties.Any()) && (other.Properties == null || !other.Properties.Any()))
            return true;
        
        // If only one dictionary is null or empty, they're not equal
        if (Properties == null || other.Properties == null)
            return false;

        // Compare keys (case-insensitive since the dictionary uses StringComparer.InvariantCultureIgnoreCase)
        if (!Properties.Keys.All(other.Properties.Keys.Contains))
            return false;

        // Compare values
        return Properties.All(kvp => 
        {
            if (!other.Properties.TryGetValue(kvp.Key, out var otherValue))
                return false;
            
            return kvp.Value?.Equals(otherValue) ?? (otherValue == null);
        });
    }

    public SchemaObject(Type t)
    {
        AddPropertiesFrom(t);
    }

    public SchemaObject AddPropertiesFrom<T>() => AddPropertiesFrom(typeof(T));

    public SchemaObject AddPropertiesFrom(Type t)
    {
        if (t == null)
            throw new ArgumentNullException(nameof(t));
        
        //Loop through all parent classes to see if they have a JsonDerivedType attribute
        var parent = t;
        var typeOptions = new List<string>();
        while (parent != null)
        {
            var attrs = parent.GetCustomAttributes<JsonDerivedTypeAttribute>();
            foreach (var attr in attrs)
            {
                if (attr.TypeDiscriminator is string s && attr.DerivedType == t)
                    typeOptions.Add(s);
            }

            parent = parent.BaseType;
        }

        if (typeOptions.Count > 0)
            Properties.Add("$type", new(PrimitiveTypes.String, choices: typeOptions));

        foreach (var prop in t.GetProperties())
        {
            var attr = prop.GetCustomAttribute<SemanticAttribute>(true);

            var isList = prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>);

            var isString = prop.PropertyType == typeof(string);
            var isStringArrayOrList = prop.PropertyType == typeof(string[]) || prop.PropertyType == typeof(List<string>);
            var isInt = prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?);
            var isIntArrayOrList = prop.PropertyType == typeof(int[]) || prop.PropertyType == typeof(List<int>);
            var isNonListClass = prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsArray && !isList;
            var isClassArrayOrList = prop.PropertyType.IsArray && prop.PropertyType.GetElementType()!.IsClass && prop.PropertyType != typeof(string[]);
            isClassArrayOrList |= isList && prop.PropertyType.GetGenericArguments()[0].IsClass && prop.PropertyType != typeof(List<string>);
            var isDouble = prop.PropertyType == typeof(double);
            var isDoubleArrayOrList = prop.PropertyType == typeof(double[]) || prop.PropertyType == typeof(List<double>);
            var isBoolean = prop.PropertyType == typeof(bool);
            var isNullable = prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            var isDateTime = prop.PropertyType == typeof(DateTime);
            var isValid = isString || isStringArrayOrList || isNonListClass || isClassArrayOrList || isBoolean || isInt || isIntArrayOrList || isDouble || isDoubleArrayOrList || isDateTime;

            if (!isValid)
                throw new InvalidOperationException($"Only string, string[], class, class[], bool, int, int[], double, double[], DateTime value types are supported for property {prop.Name} with type {prop.PropertyType.FullName}.");

            if (isClassArrayOrList)
            {
                var obj = new SchemaObject();
                var elementType = isList ? prop.PropertyType.GetGenericArguments()[0] : prop.PropertyType.GetElementType()!;
                obj.AddPropertiesFrom(elementType);
                Properties.Add(prop.Name, new(PrimitiveTypes.Array, attr?.Description, obj: obj, itemType: PrimitiveTypes.Object, anyOf: attr?.AnyOf));
                continue;
            }

            if (isNonListClass)
            {
                var obj = new SchemaObject();
                obj.AddPropertiesFrom(prop.PropertyType);
                Properties.Add(prop.Name, new(PrimitiveTypes.Object, attr?.Description, obj: obj, anyOf: attr?.AnyOf));
                continue;
            }

            var primType = isInt || isIntArrayOrList ? PrimitiveTypes.Integer : 
                isDouble || isDoubleArrayOrList ? PrimitiveTypes.Number : PrimitiveTypes.String;
            var isArray = isIntArrayOrList || isStringArrayOrList || isDoubleArrayOrList;

            if (isBoolean)
                Properties.Add(prop.Name, new(PrimitiveTypes.Boolean, attr?.Description, anyOf: attr?.AnyOf, nullable: isNullable));
            else if (isArray)
                AddProperty(prop.Name, PrimitiveTypes.Array, attr?.Description, attr?.Choices?.ToList(), attr?.AnyOf, itemType: primType, nullable: isNullable);
            else
                AddProperty(prop.Name, primType, attr?.Description, attr?.Choices?.ToList(), attr?.AnyOf, nullable: isNullable);
        }

        return this;
    }

    /// <summary>
    /// A case insensitive comparison of property names.
    /// </summary>
    public bool HasPropertyCI(string name) =>
        Properties.Any(kvp => string.Equals(kvp.Key, name, StringComparison.InvariantCultureIgnoreCase));

    public SchemaObject AddProperty(string name, string type, string description = null, List<string> choices = null, TypeDefinition[] anyOf = null, string itemType = null, bool nullable = false, SchemaObject obj = null)
    {
        if (!PrimitiveTypes.IsValid(type))
            throw new InvalidOperationException($"Invalid JSON schema primitive type: {type}");
        
        if (type == PrimitiveTypes.Array && !PrimitiveTypes.IsValid(itemType))
            throw new InvalidOperationException($"Invalid JSON schema primitive type for array item type: {itemType ?? "<null>"}");
        
        Properties.Add(name, new(type, description, choices, anyOf: anyOf, itemType: itemType, nullable: nullable, obj: obj));
        return this;
    }

    public string ToJsonString()
    {
        using var ms = new MemoryStream();
        using var jw = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
        Serialize(jw);
        jw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public void Serialize(Utf8JsonWriter jw, bool writeStartObject = true, bool includeAdditionalProperties = true, bool gemini = false, bool isNullable = false)
    {
        if (writeStartObject)
            jw.WriteStartObject();

        if (isNullable)
        {
            jw.WriteStartArray("type"u8);
            jw.WriteStringValue("object"u8);
            jw.WriteStringValue("null"u8);
            jw.WriteEndArray();
        }
        else
            jw.WriteString("type"u8, "object"u8);
        
        jw.WriteStartObject("properties"u8);
        foreach (var prop in Properties.OrderBy(p => p.Key))
        {
            jw.WritePropertyName(prop.Key);
            prop.Value.Serialize(jw, gemini);
        }
        

        jw.WriteEndObject();
        if(includeAdditionalProperties)
        {
            jw.WritePropertyName("additionalProperties"u8);
            if(AdditionalProperties == null) 
                jw.WriteBooleanValue(false);
            else 
                AdditionalProperties.Serialize(jw);
        }
        jw.WritePropertyName("required"u8);
        jw.WriteStartArray();
        foreach (var prop in Properties.OrderBy(p => p.Key))
            jw.WriteStringValue(prop.Key);
        jw.WriteEndArray();
        jw.WriteEndObject();
    }

    public void Concat(SchemaObject schemaObject)
    {
        Properties = Properties.Concat(schemaObject.Properties).ToDictionary(p => p.Key, p => p.Value);
    }
}

public class SchemaObject<T> : SchemaObject
{
    public SchemaObject() : base(typeof(T))
    {
    }
}