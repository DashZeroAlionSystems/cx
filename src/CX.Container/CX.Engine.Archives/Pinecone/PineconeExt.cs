using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives.Pinecone;

public static class PineconeExt
{
    public static string CalculatePineconeId(this TextChunk chunk) => chunk.Metadata.DocumentId != null ? $"{chunk.Metadata.DocumentId}.{chunk.SeqNo}" : chunk.Content.GetSHA256();
}
