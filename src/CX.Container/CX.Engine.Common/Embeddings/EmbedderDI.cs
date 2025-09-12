using CX.Engine.Common.Embeddings.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Embeddings;

public static class EmbedderDI
{
    public const string OpenAIEmbedderConfigurationSection = "OpenAIEmbedder";
    public const string EmbeddingCacheConfigurationSection = "EmbeddingCache";
    
    public static void AddEmbeddings(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.Configure<OpenAIEmbedderOptions>(configuration.GetSection(OpenAIEmbedderConfigurationSection));
        sc.AddSingleton<OpenAIEmbedder>();
        sc.Configure<EmbeddingCacheOptions>(configuration.GetSection(EmbeddingCacheConfigurationSection));
        sc.AddSingleton<EmbeddingCache>();
    }
}