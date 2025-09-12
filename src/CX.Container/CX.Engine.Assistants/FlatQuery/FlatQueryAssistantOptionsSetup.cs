using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.FlatQuery;

public class FlatQueryAssistantOptionsSetup : IConfigureOptions<FlatQueryAssistantOptions>
{
    private readonly IConfigurationSection _configuration;

    public FlatQueryAssistantOptionsSetup(IConfigurationSection configuration)
    {
        _configuration = configuration;
    }

    public void Configure(FlatQueryAssistantOptions options)
    {
        // Manually bind the Json properties
        options.SemanticFilterOutSchema = _configuration.GetSection(nameof(FlatQueryAssistantOptions.SemanticFilterOutSchema)).ToJsonElement();
        options.IntroSchema = _configuration.GetSection(nameof(FlatQueryAssistantOptions.IntroSchema)).ToJsonElement();
        options.IntroJsonETemplate = _configuration.GetSection(nameof(FlatQueryAssistantOptions.IntroJsonETemplate)).ToJsonNode();
        options.JsonEOutputTemplate = _configuration.GetSection(nameof(FlatQueryAssistantOptions.JsonEOutputTemplate)).ToJsonNode();
        options.SemanticSegmentMergeJsonETemplate = _configuration.GetSection(nameof(FlatQueryAssistantOptions.SemanticSegmentMergeJsonETemplate)).ToJsonNode();
    }
}