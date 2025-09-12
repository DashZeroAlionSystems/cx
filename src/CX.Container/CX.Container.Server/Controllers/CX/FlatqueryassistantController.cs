using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using CX.Engine.Assistants;
using CX.Engine.Assistants.FlatQuery;
using CX.Engine.ChatAgents;
using CX.Engine.Common.Tracing;
using CX.Engine.Common;
using Microsoft.AspNetCore.Authorization;

namespace CX.Container.Server.Controllers.CX;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AskController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AskController> _logger;

    public class AskRequest
    {
        [Required]
        public string Question { get; set; }

        [Required]
        public string AssistantName { get; set; }

        public string UserId { get; set; }

        public string SessionId { get; set; }

        public List<ChatMessage> History { get; set; } = new();

        public List<FlatQueryAssistantOptionsOverrides> Overrides { get; set; } = new();
    }

    public class AskResponse
    {
        public string Answer { get; set; }
        public bool IsStructured { get; set; }
        public string TraceId { get; set; }
    }

    public AskController(IServiceProvider serviceProvider, ILogger<AskController> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AskResponse>> Ask([FromBody] AskRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "Question cannot be empty"
                });

            if (string.IsNullOrWhiteSpace(request.AssistantName))
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "Assistant name cannot be empty"
                });

            // Get the assistant
            var assistant = _serviceProvider.GetRequiredNamedService<FlatQueryAssistant>(request.AssistantName);
            if (assistant == null)
                return NotFound(new ProblemDetails
                {
                    Title = "Assistant not found",
                    Detail = $"Assistant '{request.AssistantName}' was not found"
                });

            // Create agent request context
            var agentRequest = new AgentRequest
            {
                UserId = request.UserId ?? "anonymous",
                SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
                History = request.History ?? new List<ChatMessage>()
            };

            // Add overrides if any
            foreach (var override_ in request.Overrides)
            {
                agentRequest.Overrides.Add(override_);
            }

            // Get response from assistant
            var assistantAnswer = await assistant.AskAsync(request.Question, agentRequest);

            if (assistantAnswer is not FlatQueryAssistantAnswer flatQueryAnswer)
                throw new InvalidOperationException($"Unexpected answer type: {assistantAnswer.GetType().Name}");

            var currentTrace = CXTrace.Current;
            return Ok(new AskResponse
            {
                Answer = flatQueryAnswer.Answer,
                IsStructured = flatQueryAnswer.IsStructured,
                TraceId = currentTrace?.TraceId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ask request");
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while processing your request",
                Instance = CXTrace.Current?.TraceId
            });
        }
    }

    [HttpGet("assistants")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public ActionResult<List<string>> GetAvailableAssistants()
    {
        try
        {
            // Use reflection to get all registered FlatQueryAssistant instances
            var assistantServices = _serviceProvider.GetServices<FlatQueryAssistant>();
            return Ok(assistantServices.Select(a => a.GetType().Name).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available assistants");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving available assistants"
            });
        }
    }
}

// Extension to register the API in Startup.cs
public static class FlatQueryAssistantApiExtensions
{
    public static IServiceCollection AddFlatQueryAssistantApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        return services;
    }

    public static IEndpointRouteBuilder MapFlatQueryAssistantApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        return endpoints;
    }
}