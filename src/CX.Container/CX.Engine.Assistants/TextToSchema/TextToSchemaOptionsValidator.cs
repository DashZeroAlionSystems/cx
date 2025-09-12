using System.ComponentModel.DataAnnotations;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.TextToSchema;

public class TextToSchemaOptionsValidator : IValidatorFor<TextToSchemaOptions>
{
    private readonly IServiceProvider _sp;

    public TextToSchemaOptionsValidator([NotNull] IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public void Validate(TextToSchemaOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.OpenAIChatAgentName))
            throw new ValidationException($"{nameof(opts.OpenAIChatAgentName)} is r equired");

        if (_sp.GetNamedService<OpenAIChatAgent>(opts.OpenAIChatAgentName) == null)
            throw new ValidationException($"OpenAI Chat Agent {opts.OpenAIChatAgentName} not found");
        
        if (opts.ImageScaleFactor <= 0.1)
            throw new ValidationException($"{nameof(opts.ImageScaleFactor)} must be greater than 0.1");

        if (opts.Questions.Count > 0)
        {
            foreach (var q in opts.Questions)
                q.Validate();
            
            if (opts.ResponseSchema == null)
                throw new ValidationException($"{nameof(opts.ResponseSchema)} is required");

                if (string.IsNullOrWhiteSpace(opts.ResponseSchema.OpenAIName))
                throw new ValidationException($"{nameof(opts.ResponseSchema.OpenAIName)} is required");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(opts.ExtractionPrompt))
                throw new ValidationException($"{nameof(opts.ExtractionPrompt)} is required");

            if (opts.ResponseSchema == null)
                throw new ValidationException($"{nameof(opts.ResponseSchema)} is required");

            opts.ResponseSchema.Validate(nameof(opts.ResponseSchema));
        }
    }
}