using System.ComponentModel.DataAnnotations;
using CX.Container.Server.Common;
using CX.Engine.Assistants.Channels;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CX.Engine.Assistants.Api;

[Authorize]
[Route("api/channel")]
[ApiController]
public class ChannelController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChannelController> _logger;

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AskRequest
    {
        [Required] public string Question { get; set; }

        public string UserId { get; set; }

        public string SessionId { get; set; }

        public List<OpenAIChatMessage> History { get; set; } = new();
    }

    public ChannelController(IServiceProvider serviceProvider, ILogger<ChannelController> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("{channelId}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [RequiresAtLeastUserRole]
    public async Task<JsonResult> Ask(
        string channelId,
        [FromBody] AskRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Question cannot be empty",
                    details = "The 'Question' field is required and must not be empty",
                    timestamp = DateTime.UtcNow
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            Channel channel;
            try
            {
                channel = _serviceProvider.GetRequiredNamedService<Channel>(channelId);
            }
            catch (Exception ex)
            {
                return new(new
                {
                    success = false,
                    error = $"Channel '{channelId}' not found",
                    details = ex.Message,
                    timestamp = DateTime.UtcNow
                })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Create agent request with history
            var agentRequest = new AgentRequest
            {
                UserId = request.UserId ?? "anonymous",
                SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
                History = request.History?.Select(ChatMessage (h) => h).ToList() ?? []
            };

            var result = await channel.Assistant.AskAsync(request.Question, agentRequest);

            return new(new
            {
                success = true,
                result = MiscHelpers.ParseJsonOrString(result.Answer),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            throw;
        }
    }
}