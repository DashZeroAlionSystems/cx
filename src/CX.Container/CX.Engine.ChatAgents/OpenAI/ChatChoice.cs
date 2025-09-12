using System.Text.Json;
using CX.Engine.Common.Json;
using JetBrains.Annotations;

namespace CX.Engine.ChatAgents.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ChatChoice : IChatChoice
{
    public ChatMessage ChatMessage { get; set; }
    public string FinishReason { get; set; }

    public void PopulateFromJsonReader(ref Utf8JsonReader jr)
    {
        jr.ReadObjectProperties(this, false,
            (ref Utf8JsonReader jr, ChatChoice choice, string name) =>
            {
                switch (name)
                {
                    case "message":
                        jr.Read(JsonTokenType.StartObject);
                        choice.ChatMessage = OpenAIChatMessage.FromJsonReader(ref jr);
                        break;
                    case "finish_reason":
                        var finish_reason = jr.ReadStringValue();
                        choice.FinishReason = finish_reason;
                        break;
                    default:
                        jr.SkipPropertyValue();
                        break;
                }
            });
    }
}