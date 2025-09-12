using CX.Engine.Assistants.Channels;
using CX.Engine.Assistants.Walter1;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common.Migrations;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Registration.Full;

public static class FullProfileMigrations
{
    public static Migration AddChannelForDefaultChatbot = new(async sp =>
    {
        var channels = sp.GetRequiredService<TypedJsonStore<ChannelOptions>>();
        await channels.SetIfNotExistsAsync("ui", new()
        {
            AssistantName = "walter-1.default",
            DisplayName = "Default Chatbot",
            ShowInUI = true
        });
    });
    
    public const string BusinessGPT = "business-gpt";
    
    public static Migration AddBusinessGPT = new(async sp =>
    {
        var channels = sp.GetRequiredService<TypedJsonStore<ChannelOptions>>();
        var walter1s = sp.GetRequiredService<TypedJsonStore<Walter1AssistantOptions>>();
        var openais = sp.GetRequiredService<TypedJsonStore<OpenAIChatAgentOptions>>();
        await openais.SetIfNotExistsAsync(BusinessGPT, new()
        {
            Model = "gpt-4o-mini",
            APIKey = null,
            MaxConcurrentCalls = 20,
            StripMarkdownLinks = false
        });
        await channels.SetIfNotExistsAsync("business-gpt", new()
        {
            AssistantName = "walter-1.business-gpt",
            DisplayName = "Business GPT",
            ShowInUI = true
        });
        await walter1s.SetIfNotExistsAsync("business-gpt", new()
        {
            MinSimilarity = 0.25,
            CutoffContextTokens = 9000,
            MaxChunksPerAsk = null,
            CutoffHistoryTokens = 9000,
            ChatAgent = "OpenAI.business-gpt",
            DefaultSystemPrompt = "",
            DefaultContextualizePrompt = "",
            Archive = null,
            Archives = null,
            InputProcessors = null,
            TopDocumentLimit = null,
            SortChunks = true,
            UseAttachments = false
        });
    });
    
    public static void AddFullProfileMigrations(this IServiceCollection sc)
    {
        sc.AddMigrations(
            AddChannelForDefaultChatbot,
            AddBusinessGPT);
    }
}