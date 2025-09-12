using System.Text.Json;
using CX.Engine.ChatAgents.OpenAI.Schemas;

namespace CX.Engine.Assistants;

public interface IAssistant
{
    Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx);
    
    async Task<(T obj, AssistantAnswer answer)> AskAsync<T>(string question, AgentRequest astCtx, bool useOverride = true)
    {
        var scoped = astCtx.GetScoped();
        if (useOverride)
            scoped.Overrides.Add(new ResponseFormatOverride(new OpenAISchema<T>()));
        
        var res = await AskAsync(question, scoped);

        if (res.IsRefusal)
            return (default, res);
        
        var obj = JsonSerializer.Deserialize<T>(res.Answer!);
        return (obj, res);
    }
}
