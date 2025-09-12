using System.Text.Json;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Assistants;
using CX.Engine.Assistants.Walter1;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.SharedOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Controllers.CX
{
    [Authorize]
    [ApiController]
    [Route("api/assistants/walter1")]
    [ApiVersion("1.0")]
    public sealed class Walter1AssistantController(
        IServiceProvider sp,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService) : ControllerBase
    {
        public class AskRequest
        {
            public string Question { get; set; }
            public string AssistantId { get; set; }
            public AgentRequest Context { get; set; }
            public Walter1AssistantOptionsOverrides Overrides { get; set; }
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("ask")]
        public async Task<ActionResult<AssistantAnswer>> AskAsync([FromBody] AskRequest request)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute
            
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { Message = "Question cannot be empty" });

            if (string.IsNullOrWhiteSpace(request.AssistantId))
                return BadRequest(new { Message = "AssistantId cannot be empty" });

            try
            {
                var assistant = sp.GetNamedService<Walter1Assistant>(request.AssistantId);
                if (assistant == null)
                    return NotFound(new { Message = $"Assistant with id '{request.AssistantId}' not found" });

                // Set up context with defaults if not provided
                var context = request.Context ?? new AgentRequest();

                // Add overrides to context if provided
                if (request.Overrides != null)
                {
                    context.Overrides.Add(request.Overrides);
                }

                var answer = await assistant.AskAsync(request.Question, context);
                return Ok(answer);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request", Error = ex.Message });
            }
        }

        [HttpPost]
        [RequiresAtLeastUserRole]
        [Route("{assistantId}/ask")]
        public async Task<ActionResult<AssistantAnswer>> AskByIdAsync(
            string assistantId,
            [FromBody] string question,
            [FromQuery] string contextJson = null,
            [FromQuery] string overridesJson = null)
        {
            try
            {
                var context = string.IsNullOrEmpty(contextJson)
                    ? new AgentRequest()
                    : JsonSerializer.Deserialize<AgentRequest>(contextJson);

                var overrides = string.IsNullOrEmpty(overridesJson)
                    ? null
                    : JsonSerializer.Deserialize<Walter1AssistantOptionsOverrides>(overridesJson);

                var request = new AskRequest
                {
                    Question = question,
                    AssistantId = assistantId,
                    Context = context,
                    Overrides = overrides
                };

                return await AskAsync(request);
            }
            catch (JsonException)
            {
                return BadRequest(new { Message = "Invalid JSON in context or overrides" });
            }
        }
    }
}