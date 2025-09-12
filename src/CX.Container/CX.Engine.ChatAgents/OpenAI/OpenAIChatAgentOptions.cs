using System.Text.Json.Serialization;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace CX.Engine.ChatAgents.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class OpenAIChatAgentOptions : IValidatableConfiguration
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string APIKey { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int MaxConcurrentCalls { get; set; }
    public bool StripMarkdownLinks { get; set; }
    public bool ApplyCachedTokenDiscountToInputTokens { get; set; } = true;
    public bool OnlyUserRole { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
    public double DefaultTemperature { get; set; } = 0.3;
    public TimeSpan BackOffTimeOut { get; set; } = TimeSpan.FromSeconds(10);
    public bool EnableCaching { get; set; }
    public string CachePostgreSQLClientName { get; set; }
    public string CacheTableName { get; set; }

    public Crc32JsonStore.StoreIdentifier StoreId => (CachePostgreSQLClientName, CacheTableName);

    public void Validate(IConfigurationSection section)
    {
        section.ThrowIfNullOrWhiteSpace(APIKey);
        section.ThrowIfNull(Model);
        if (EnableCaching)
        {
            section.ThrowIfNullOrWhiteSpace(CachePostgreSQLClientName,
                message: $"{nameof(CachePostgreSQLClientName)} is required when {nameof(EnableCaching)} is true in {section.Path}");
            section.ThrowIfNullOrWhiteSpace(CacheTableName,
                message: $"{nameof(CacheTableName)} is required when {nameof(EnableCaching)} is true in {section.Path}");
        }

        section.ThrowIfZeroOrNegative(MaxConcurrentCalls);
    }
}