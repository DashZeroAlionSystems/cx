using System.Text.Json;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common.Json;
using Microsoft.Extensions.Logging;

namespace CX.Engine.ChatAgents.Gemini;

public class GeminiChatResponse : ChatResponseBase
{
    public override bool IsRefusal 
    {
        get
        {
            if (Choices.Count == 0 || Choices[0].ChatMessage == null)
                return false;

            return Choices[0].ChatMessage!.Refusal != null;
        }
    }

    public override string Answer
    {
        get
        {
            if (Choices.Count == 0 || Choices[0].ChatMessage == null)
                return null;

            return Choices[0].ChatMessage!.Content ?? Choices[0].ChatMessage!.Refusal;
        }
        set
        {
            if (value == null)
                throw new InvalidOperationException($"Cannot clear the answer of a {nameof(GeminiChatResponse)}.");
            Choices[0].ChatMessage!.Content = value;
        }
    }
    
    public override void PopulateFromBytes(byte[] bytes)
    {
        var jr = new Utf8JsonReader(bytes);

        jr.Read();
        jr.TokenMustBe(JsonTokenType.StartObject);

        jr.ReadObjectProperties(this,
            false,
            (ref Utf8JsonReader jr, GeminiChatResponse response, string name) =>
            {
                switch (name)
                {
                    case "candidates":
                        jr.ReadArrayOfObject(true,
                            (ref Utf8JsonReader jr) =>
                            {
                                var choice = new GeminiChatChoice();
                                choice.PopulateFromJsonReader(ref jr);
                                response.Choices.Add(choice);
                            });
                        break;
                    case "created":
                        response.Created = jr.ReadInt64Value();
                        break;
                    case "id":
                        response.Id = jr.ReadStringValue();
                        break;
                    case "model":
                        response.Model = jr.ReadStringValue();
                        break;
                    case "object":
                        response.Object = jr.ReadStringValue();
                        break;
                    case "usageMetadata":
                        response.ChatUsage = JsonSerializer.Deserialize<GeminiChatUsage>(ref jr)!;
                        break;
                    default:
                        jr.SkipPropertyValue();
                        break;
                }
            });
    }

    public override ChatResponse ToChatResponse(ILogger logger, OpenAIChatAgentOptions options)
    {
        throw new NotImplementedException();
    }
}