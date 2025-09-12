using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CX.Engine.ChatAgents.OpenAI;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class OpenAIChatResponse: ChatResponseBase
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

            var finishReason = ((ChatChoice)Choices[0]).FinishReason; 
            switch (finishReason.NullIfWhiteSpace())
            {
                case null:
                case "stop":
                    break;
                case "length":
                    throw new OpenAIException("The maximum number of tokens specified in the request was reached");
                case "content_filter":
                    throw new OpenAIException("Content was omitted due to a flag from our content filters");
                default:
                    throw new ArgumentException($"FinishReason != stop (found: {finishReason})");
            }
            
            return Choices[0].ChatMessage!.Content ?? Choices[0].ChatMessage!.Refusal;
        }
        set
        {
            if (value == null)
                throw new InvalidOperationException($"Cannot clear the answer of a {nameof(OpenAIChatResponse)}.");

            if (Choices.Count == 0)
                Choices.Add(new ChatChoice() {
                    ChatMessage = new OpenAIChatMessage ("asssitant", "")
                });
            Choices[0].ChatMessage!.Content = value;
        }
    }

    public List<ToolCall> ToolCalls
    {
        get
        {
            if (Choices.Count == 0 || Choices[0].ChatMessage == null)
                return null;
            return Choices[0].ChatMessage!.ToolCalls;
        }
    }

    public override ChatResponse ToChatResponse(ILogger logger, OpenAIChatAgentOptions options)
    {
        var res = new ChatResponse();
        res.Answer = Answer!;
        res.SystemPrompt = SystemPrompt;
        res.ToolCalls = Choices[0].ChatMessage!.ToolCalls;

        if (res.Answer != null && InputAttachments != null)
            foreach (var link in MarkdownLinkExtractor.ExtractLinks(res.Answer))
            {
                var att = InputAttachments.FirstOrDefault(a => a.FullUrl == link.Url);

                if (att != null)
                {
                    if (options.StripMarkdownLinks)
                      res.Answer = res.Answer.Replace(link.full, $"[{link.linkText}] ({att.FullUrl})");
                    
                    if (res.Attachments?.All(a => a.FullUrl != link.Url) ?? true)
                        res.Attachments = (res.Attachments ?? []).Concat([att]).ToArray();
                }
            }

        // Handling for no-longer supported tool calls
        // if (Choices[0]?.ChatMessage is { ToolCalls: not null })
        // {
        //     foreach (var toolcall in Choices[0].ChatMessage!.ToolCalls)
        //     {
        //         if (toolcall.Name != "attach")
        //         {
        //             logger?.LogWarning("Invalid tool name received: {name}", toolcall.Name);
        //             continue;
        //         }
        //
        //         if (string.IsNullOrWhiteSpace(toolcall.Arguments))
        //         {
        //             logger?.LogWarning("Toolcall with no arguments received");
        //             continue;
        //         }
        //
        //         var jr = new Utf8JsonReader(Encoding.UTF8.GetBytes(toolcall.Arguments));
        //         jr.Read(JsonTokenType.StartObject);
        //         //Get the file_id property
        //         while (jr.TokenType != JsonTokenType.EndObject)
        //         {
        //             jr.Read();
        //
        //             if (jr.TokenType != JsonTokenType.PropertyName)
        //                 continue;
        //
        //             var propertyName = jr.GetString();
        //             jr.Read();
        //             if (propertyName == "file_id")
        //             {
        //                 var id = jr.GetString()!;
        //                 if (id == null)
        //                 {
        //                     logger?.LogWarning("Null file_id received");
        //                     continue;
        //                 }
        //
        //                 var att = InputAttachments?.FirstOrDefault(ia => ia.FileId == id);
        //
        //                 if (att == null)
        //                 {
        //                     logger?.LogWarning("Attachment with file_id {fileId} not found", id);
        //                     continue;
        //                 }
        //
        //                 if (res.Attachments == null)
        //                     res.Attachments = [att];
        //                 else
        //                     res.Attachments = res.Attachments.Concat(new[] { att }).ToArray();
        //             }
        //         }
        //     }
        // }

        return res;
    }

    public override void PopulateFromBytes(byte[] bytes)
    {
        var jr = new Utf8JsonReader(bytes);

        jr.Read();
        jr.TokenMustBe(JsonTokenType.StartObject);

        jr.ReadObjectProperties(this,
            false,
            (ref Utf8JsonReader jr, OpenAIChatResponse response, string name) =>
            {
                switch (name)
                {
                    case "choices":
                        jr.ReadArrayOfObject(true,
                            (ref Utf8JsonReader jr) =>
                            {
                                var choice = new ChatChoice();
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
                    case "usage":
                        response.ChatUsage = JsonSerializer.Deserialize<ChatUsage>(ref jr)!;
                        break;
                    default:
                        jr.SkipPropertyValue();
                        break;
                }
            });
    }
}