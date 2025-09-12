using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.TextProcessors.Tests;

public class CharLimitSplitterTests
{
    [Fact]
    public async Task ChunkingTests()
    {
        var chunker = new CharLimitSplitter();
        chunker.CharLimit = 3;

        //Case with perfect with
        {
            var chunks = await chunker.ChunkAsync(new("How are you?"));
            Assert.True(chunks.SequenceEqual(["How", " ar", "e y", "ou?"]));
        }
        
        //Case with shorter last segment
        {
            var chunks = await chunker.ChunkAsync(new("How is Bob?"));
            Assert.True(chunks.SequenceEqual(["How", " is", " Bo", "b?"]));
        }
        
        //Single segment
        {
            var chunks = await chunker.ChunkAsync(new("Hi"));
            Assert.True(chunks.SequenceEqual(["Hi"]));
        }
        
        //Empty
        {
            var chunks = await chunker.ChunkAsync(new(""));
            Assert.True(chunks.SequenceEqual([]));
        }
    }
}