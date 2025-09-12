using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CX.Engine.Assistants.TextToSchema;

public class TextToSchemaOptionsSetup : IConfigureOptions<TextToSchemaOptions>
{
    private readonly IConfigurationSection _configuration;
    
    public TextToSchemaOptionsSetup(IConfigurationSection configuration)
    {
        _configuration = configuration;
    }

    public void Configure(TextToSchemaOptions options)
    {
        if (_configuration.GetSection(nameof(TextToSchemaOptions.ResponseSchema)).Exists())
        {
            options.ResponseSchema ??= new();
            options.ResponseSchema.Setup(_configuration.GetSection(nameof(TextToSchemaOptions.ResponseSchema)));
        }

        options.Questions = [];
        var qs = _configuration.GetSection(nameof(options.Questions));
        if (qs.Exists())
        {
            foreach (var qe in qs.IterateArray())
            {
                var q = new TextToSchemaQuestion();
                qe.Bind(q);
                q.Setup(qe);
                options.Questions.Add(q);
            }
        }
    }
}