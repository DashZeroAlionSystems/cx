using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.ChatAgents;

public static class ChatCacheDI
{
    public const string ConfigurationSection = "ChatCache";
    
    public static void AddChatCache(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<ChatCacheOptions>(configuration.GetSection(ConfigurationSection));
        sc.AddSingleton<ChatCache>();
    }
}