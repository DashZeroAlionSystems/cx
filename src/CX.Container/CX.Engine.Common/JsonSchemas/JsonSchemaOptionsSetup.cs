using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.JsonSchemas;

public class JsonSchemaOptionsSetup : IConfigureOptions<JsonSchemaOptions>
{
    private readonly IConfigurationSection _configuration;

    public JsonSchemaOptionsSetup(IConfigurationSection configuration)
    {
        _configuration = configuration;
    }

    public void Configure(JsonSchemaOptions options)
    {
        options.Schema = _configuration.ToJsonElement();
    }
}