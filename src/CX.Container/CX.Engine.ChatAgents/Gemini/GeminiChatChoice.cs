using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiChatChoice : IChatChoice
{
    public ChatMessage ChatMessage { get; set; }

    public void PopulateFromJsonReader(ref Utf8JsonReader jr)
    {
        jr.ReadObjectProperties(this, false,
            (ref Utf8JsonReader jr, GeminiChatChoice choice, string name) =>
            {
                switch (name)
                {
                    case "content":
                        jr.Read(JsonTokenType.StartObject);
                        choice.PopulateFromJsonReader(ref jr);
                        break;
                    case "parts":
                        jr.ReadArrayOfObject(true,
                            (ref Utf8JsonReader jr) =>
                            {
                                choice.ChatMessage = GeminiChatMessage.FromJsonReader(ref jr);
                            });
                        break; 
                    case "error":
                        jr.Read(JsonTokenType.StartObject);
                        choice.ChatMessage = GeminiChatMessage.FromJsonReader(ref jr);
                        break;
                    default:
                        jr.SkipPropertyValue();
                        break;
                }
            });
    }
}