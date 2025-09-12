using JetBrains.Annotations;

namespace CX.Engine.ChatAgents;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ChatCacheOptions
{
    public string CacheFile { get; set; }
    public bool UseCache { get; set; }
    public string LogPath { get; set; }
}