using CX.Engine.Common.Stores.Json;
using CX.Engine.TextProcessors.Splitters;

namespace CX.Engine.ChatAgents;

public interface IChatAgent
{
    string Model { get; }

    Task<ChatResponseBase> RequestAsync(ChatRequestBase ctx);
    ChatRequestBase GetRequest(string question = null, List<TextChunk> chunks = null, string systemPrompt = null);
    ChatRequestBase GetRequest<T>(string question = null, List<TextChunk> chunks = null, string systemPrompt = null);
    SchemaBase GetSchema(string name);
}

public interface IChatAgent<TRequest, TResponse> : IChatAgent
    where TRequest: ChatRequestBase
    where TResponse : ChatResponseBase
{
    async Task<TResponse> RequestAsync(TRequest req) => (TResponse)await ((IChatAgent)this).RequestAsync(req); 

    async Task<TResponse> RequestAsync(Action<TRequest> build)
    {
        var ctx = GetRequest();
        build((TRequest)ctx);
        return (TResponse)await RequestAsync(ctx);
    }
    
    async Task<TResponse> RequestAsync(string question,
        List<TextChunk> chunks = null,
        string systemPrompt = null) => await RequestAsync(ctx =>
    {
        ctx.Question = question;
        ctx.Chunks = chunks;
        ctx.SystemPrompt = systemPrompt;
    });
    
    
}