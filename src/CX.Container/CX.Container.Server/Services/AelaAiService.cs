using CX.Container.Server.Configurations;
using CX.Container.Server.Domain.MessageCitations.Dtos;
using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Mappings;
using CX.Container.Server.Domain.Messages.Models;
using CX.Engine.Common;
using CX.Engine.Assistants;
using CX.Engine.Assistants.Channels;
using CX.Engine.ChatAgents.OpenAI;
using Flurl;
using Microsoft.Extensions.Options;
using Flurl.Http;
using Newtonsoft.Json;

namespace CX.Container.Server.Services;

public interface IAiService
{
    Task<AelaResponseDto> SendMessage(MessageForCreation question);

    Task<bool> RateMessageAsync(Guid threadId, int rating);
}

public class AiService(
    IConversationCache cache,
    IOptions<AiOptions> aiOptions,
    ILogger<AiService> logger,
    IServiceProvider sp) : IAiService
{
    private readonly IConversationCache _cache = cache;
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly ILogger<AiService> _logger = logger;
    private readonly IServiceProvider _sp = sp;

    public async Task<AelaResponseDto> SendMessage(MessageForCreation question)
    {
        var channelName = !string.IsNullOrWhiteSpace(question.ChannelName) ? question.ChannelName : _aiOptions.ChannelName;
        IAssistant? assistant = null;
        ChannelOptions? channelOptions = null;

        if (!string.IsNullOrWhiteSpace(channelName) || string.Equals(channelName, "python", StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                var channel = _sp.GetRequiredNamedService<Channel>(channelName);
                lock (channel.OptionsChangeLock)
                {
                    assistant = channel.Assistant;
                    channelOptions = channel.Options;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attemping to find channel");
                return new()
                {
                    Message = $"Could not find the channel {channelName}",
                };
            }
        }
        var conversation = new ConversationDto();
        if (question.ThreadId.HasValue)
        {
            conversation = question.ToConversationDto();
            conversation.History = await _cache.GetConversation(question.ThreadId!.Value);
        }

        AelaResponseDto answer;

        if (assistant != null)
        {
            var ctx = new AgentRequest
            {
                UserId = "user",
                //We have to have a SessionId for logging to work towards Context AI.
                SessionId = question.ThreadId?.ToString() ?? Guid.NewGuid().ToString()
            };
            ctx.Overrides.AddRange(channelOptions.Overrides);

            if (conversation.History != null)
                foreach (var entry in conversation.History)
                    ctx.History.Add(new OpenAIChatMessage(entry.FromUser ? "user" : "assistant", entry.Message));

            var res = await assistant.AskAsync(question.Content, ctx);

            answer = new AelaResponseDto
            {
                Message = res.Answer,
                Citations = res.Attachments?.Select(att => new MessageCitationDto
                {
                    Name = att.FileName,
                    Url = att.FileUrl,
                    Type = "form",
                }).ToArray()
            };
        }
        else
        {
            answer = await _aiOptions.ChatUrl
                .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                .WithHeader("x-api-key", _aiOptions.ChatApiKey)
                .WithHeader("Content-Type", "application/json")
                .PostJsonAsync(conversation)
                .ReceiveJson<AelaResponseDto>();
        }
        if (question.ThreadId.HasValue)
        {
            conversation.History.Add(question.ToMessageForChatDto());
            conversation.History.Add(new MessageForChatDto { FromUser = false, Message = answer.Message });

            await _cache.SetConversation(question.ThreadId.Value, conversation.History);
        }

        return answer;
    }

    public async Task<bool> RateMessageAsync(Guid threadId, int rating)
    {
        try
        {
            var json = JsonConvert.SerializeObject(new
            {
                rating = rating,
                thread_id = threadId.ToString()
            }, new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            });

            var content = new StringContent(json, null, "application/json");
            var answer = await _aiOptions.AiServerUrl
                .AppendPathSegment("v1")
                .AppendPathSegment("chat")
                .AppendPathSegment("rate")
                .WithHeader("x-api-key", _aiOptions.ChatApiKey)
                .WithHeader("Content-Type", "application/json")
                .PostAsync(content);

            var status = answer.ResponseMessage.IsSuccessStatusCode;
            _logger.LogInformation($"Result user sentiment: {status}");

            return status;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Rate Message");
        }

        return false;
    }
}