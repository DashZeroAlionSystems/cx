using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CX.Engine.Common.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.Json;

public class JsonOptionsSetup<T> : IConfigureOptions<T> where T : class
{
    public readonly IConfigurationSection Root;
    public Action<T> AfterConfigure;

    public JsonOptionsSetup([NotNull] IConfigurationSection root, Action<T> afterConfigure = null)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        AfterConfigure = afterConfigure;
    }

    public void Configure(T options)
    {
        foreach (var field in typeof(T).GetFieldsAndPropertiesWithAttribute<UseJsonDocumentSetupAttribute>())
        {
            var jsonOptions = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter() }
            };

            if (field.TryGetAttribute<UseJsonTypeResolver>(out var typeResolverAttr))
            {
                var converter = (IJsonTypeInfoResolver)Activator.CreateInstance(typeResolverAttr.ResolverType);
                jsonOptions.TypeInfoResolver = converter;
            }

            object defaultValue = null;
            
            if (field.GetAttribute<UseNewInstanceForDefaultValueAttribute>() != null)
                defaultValue = Activator.CreateInstance(field.ValueType);

            var jsonDocSection = Root.GetSection(field.Name);
            if (jsonDocSection.Exists())
            {
                var jsonDoc = jsonDocSection.ToJsonDocument();

                object value;
                
                if (field.ValueType == typeof(JsonNode))
                    value = JsonNode.Parse(JsonSerializer.Serialize(jsonDoc));
                else
                    value = jsonDoc.Deserialize(field.ValueType, jsonOptions);
                
                field.SetValue(options, value ?? defaultValue);
            }
            else
                field.SetValue(options, defaultValue);
        }

        AfterConfigure?.Invoke(options);
    }

    public static JsonOptionsSetup<T> Factory(IConfigurationSection section) => new(section);
}