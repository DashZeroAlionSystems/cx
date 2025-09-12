using CX.Engine.Archives;

namespace CX.Engine.Assistants;

public interface IUsesArchive
{
    IChunkArchive ChunkArchive { get; }
}