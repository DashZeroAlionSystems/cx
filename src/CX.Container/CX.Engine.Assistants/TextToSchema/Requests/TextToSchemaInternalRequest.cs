using CX.Engine.Common.Tracing;

namespace CX.Engine.Assistants.TextToSchema.Requests;

internal class TextToSchemaInternalRequest : TextToSchemaRequestBase
{
    public string Text;
    public byte[] ImageBytes;
    public CXTrace Trace;
}