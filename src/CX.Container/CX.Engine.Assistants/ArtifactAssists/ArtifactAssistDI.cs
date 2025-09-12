using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants.ArtifactAssists;

public static class ArtifactAssistDI
{
    public static void AddArtifactAssist(this IServiceCollection services)
    {
        services.AddSingleton<ArtifactAssist>();
    } 
}