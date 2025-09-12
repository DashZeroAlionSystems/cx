namespace CX.Engine.TextProcessors.Splitters;

public class CharLimitSplitter
{
    public int CharLimit = 50_000;
    
    public Task<List<string>> ChunkAsync(string document)
    {
        var chunks = new List<string>();
        var i = 0;
        
        while (i < document.Length)
        {
            var len = Math.Min(CharLimit, document.Length - i);
            var chunk = document.Substring(i, len);
            chunks.Add(chunk);
            i += len;
        }
        
        return Task.FromResult(chunks);
    }
}