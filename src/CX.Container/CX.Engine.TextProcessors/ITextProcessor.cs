namespace CX.Engine.TextProcessors;

public interface ITextProcessor
{
    Task<string> ProcessAsync(string text);
}