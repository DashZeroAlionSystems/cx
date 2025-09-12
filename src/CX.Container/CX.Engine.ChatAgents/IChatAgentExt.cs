using System.Text.Json;

namespace CX.Engine.ChatAgents;

public static class IChatAgentExt
{
    public static ChatRequestBase GetRequest<T>(this IChatAgent agent) => agent.GetRequest().WithResponseType<T>(agent);

    public static async Task<JsonDocument> RequestJsonDocAsync(this IChatAgent agent, ChatRequestBase req)
    {
        var res = await agent.RequestAsync(req);
        return JsonDocument.Parse(res.Answer);
    }

    public static async Task<T> RequestAsync<T>(this IChatAgent agent, ChatRequestBase req)
    {
        var res = await agent.RequestAsync(req);
        return JsonSerializer.Deserialize<T>(res.Answer);
    }

    public static async Task<T> RequestAsync<T>(this IChatAgent agent, string question)
    {
        var res = await agent.RequestAsync(agent.GetRequest<T>(question));
        return JsonSerializer.Deserialize<T>(res.Answer);
    }
}