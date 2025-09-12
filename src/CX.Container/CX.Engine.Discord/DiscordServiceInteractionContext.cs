using Discord.Interactions;
using Discord.WebSocket;

namespace CX.Engine.Discord;

public sealed class DiscordServiceInteractionContext : SocketInteractionContext
{
    public DiscordService.Snapshot Snapshot;
    
    public DiscordServiceInteractionContext(DiscordService.Snapshot snapshot, DiscordSocketClient client, SocketInteraction interaction) : base(client, interaction)
    {
        Snapshot = snapshot;
    }
}