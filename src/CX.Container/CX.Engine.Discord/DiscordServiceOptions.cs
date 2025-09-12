using CX.Engine.Common;
using JetBrains.Annotations;

namespace CX.Engine.Discord;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DiscordServiceOptions : IValidatable
{
    public string Token { get; set; } = null!;
    
    public Dictionary<string, DiscordChannelOptions> Channels { get; set; } = [];

    public DiscordChannelOptions GetChannelOptionsByDiscordId(ulong discordId) =>
        Channels.Values.FirstOrDefault(c => c.DiscordId == discordId)!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Token))
            throw new InvalidOperationException($"{nameof(Token)} is required.");

        foreach (var kvp in Channels)
        {
            kvp.Value.Name = kvp.Key;
            kvp.Value.Validate();
        }
    }
}