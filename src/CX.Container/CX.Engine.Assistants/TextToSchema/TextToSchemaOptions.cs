using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;

namespace CX.Engine.Assistants.TextToSchema;

[ValidatedBy(typeof(TextToSchemaOptionsValidator))]
public class TextToSchemaOptions
{
    public string ExtractionPrompt { get; set; }
    public string OpenAIChatAgentName { get; set; }
    public OpenAIJsonSchemaDefinition ResponseSchema { get; set; }
    public List<TextToSchemaQuestion> Questions { get; set; }
    public bool IncludeQuestionReasoning { get; set; }
    public bool WriteIndented { get; set; }
    public bool ReturnsArray { get; set; }
    public double ImageScaleFactor { get; set; } = 4;
}