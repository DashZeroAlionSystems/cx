using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.Archives;

public class Entry(TextChunk chunk, float[] embedding)
{
    public readonly TextChunk Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
    public readonly float[] Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));

    public override string ToString() => Chunk.ToString();
}