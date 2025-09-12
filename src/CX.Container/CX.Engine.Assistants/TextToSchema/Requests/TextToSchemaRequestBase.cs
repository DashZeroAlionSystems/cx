namespace CX.Engine.Assistants.TextToSchema.Requests;

public class TextToSchemaRequestBase : AgentRequest
{
    public Dictionary<string, string> Parameters;

    public override void Assign(AgentRequest source)
    {
        base.Assign(source);
        if (source is TextToSchemaRequestBase src)
            Parameters = src.Parameters;
    }
}