using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.ChatAgents.OpenAI;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.JsonSchemas;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class OpenAIJsonSchemaDefinition
{
    public string OpenAIName { get; set; }
    public string Name { get; set; }
    public JsonElement Schema { get; set; }

    private JsonSchemaOptions _jsonSchemaOptions;

    public void Setup(IConfigurationSection section)
    {
        Schema = section.GetSection(nameof(Schema)).ToJsonElement();
    }

    public void Validate(string sectionName)
    {
        if (string.IsNullOrWhiteSpace(OpenAIName))
            throw new InvalidOperationException($"{sectionName}.{nameof(OpenAIName)} is required");
        
        if (string.IsNullOrWhiteSpace(Name) && Schema.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"{sectionName}.{nameof(Name)} or {nameof(Schema)} is required");
    }

    public JsonElement GetAndMonitorSchema<T>(IConfiguration _config, ILogger _logger, SnapshotOptionsMonitor<T> optionsMonitor, IDisposeTracker disposeTracker, IServiceProvider sp) 
    {
        if (!string.IsNullOrWhiteSpace(Name))
        {
            var schemaMonitor = JsonSchemaStoreDI.MonitorSchema(Name, _config);

            if (schemaMonitor.CurrentValue is null)
                throw new InvalidOperationException($"Schema '{Name}' not found");

            disposeTracker.TrackDisposable(schemaMonitor.Snapshot(() => _jsonSchemaOptions, schema => _jsonSchemaOptions = schema,
                _ => optionsMonitor.NotifyChange(), _logger, sp));
            return Schema;
        }
        else
            return Schema;
    }

    public JsonNode GetAndMonitorOpenAISchema<T>(IConfiguration _config, ILogger _logger, SnapshotOptionsMonitor<T> optionsMonitor,
        IDisposeTracker disposeTracker, IServiceProvider sp) 
    {
        var schema = GetAndMonitorSchema(_config, _logger, optionsMonitor, disposeTracker, sp);
        return OpenAIChatAgent.WrapSchema(JsonObject.Create(schema), OpenAIName);
    }
}