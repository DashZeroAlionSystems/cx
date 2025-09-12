using CX.Engine.ChatAgents.Gemini;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.ChatAgents;

public static class ChatAgentsDI
{
    public const string Engine_OpenAI = "OpenAI";
    public const string Engine_Gemini = "Gemini";
    
    public static void AddChatAgents(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddOpenAIChatAgents(configuration);
        sc.AddGeminiChatAgents(configuration);
        sc.AddNamedSingletons<IChatAgent>(configuration, static (sp, _, name, optional) =>
        {
            var (engine, subName) = name.SplitAtFirst(".");

            switch (engine)
            {
                case Engine_OpenAI:
                    return sp.GetNamedService<OpenAIChatAgent>(subName, optional);
                case Engine_Gemini:
                    return sp.GetNamedService<GeminiChatAgent>(subName, optional);
                default:
                    throw new NotSupportedException($"Chat engine '{engine}' is not supported.");
            }
        });
    }
}