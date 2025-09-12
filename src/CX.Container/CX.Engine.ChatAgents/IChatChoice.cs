using System.Text.Json;

namespace CX.Engine.ChatAgents;

public interface IChatChoice
{
    
    public ChatMessage ChatMessage { get; set; }

    public void PopulateFromJsonReader(ref Utf8JsonReader jr);
}