using System.Collections.Concurrent;

namespace CX.Engine.Discord;

public static class UserContexts
{
    private static readonly ConcurrentDictionary<ulong, UserContext> Contexts = new();
    
    public static UserContext Get(ulong userId, string username)
    {
        return Contexts.GetOrAdd(userId, _ => new() { UserId = username } );
    }
}