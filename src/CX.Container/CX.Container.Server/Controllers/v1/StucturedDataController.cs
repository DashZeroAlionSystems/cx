using CX.Container.Server.Domain.Messages.Models;
using CX.Container.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CX.Container.Server.Options;
using CX.Engine.SharedOptions; // For accessing appsettings

namespace CX.Container.Server.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    public class StructuredDataController(IAiService? aiService, IOptions<StructuredDataOptions> structuredDataOptions) : ControllerBase
    {
        private readonly IAiService? _aiService = aiService;
        private readonly StructuredDataOptions _structuredDataOptions = structuredDataOptions.Value;

        // GET: api/<StructuredDataController>
        [HttpGet]
        public async Task<IActionResult> AskWithoutHistory(string QuestionText, string ChannelName)
        {
            if (!ValidateApiKeyAndSecret(Request))
            {
                return Unauthorized("Invalid API Key or Secret");
            }

            if (_aiService == null)
            {
                return BadRequest("AI Service is not available");
            }

            var question = new MessageForCreation()
            {
                ThreadId = default,
                ChannelName = string.IsNullOrWhiteSpace(ChannelName) ? "Walter1" : ChannelName,
                Content = QuestionText,
                ContentType = "text/plain",
                MessageType = "question"
            };

            var aiResponse = await _aiService.SendMessage(question);

            return Ok(aiResponse.Message);
        }

        [HttpGet("Detailed")]
        public async Task<IActionResult> AskWithoutHistoryDetailed(string QuestionText, string ChannelName)
        {
            if (!ValidateApiKeyAndSecret(Request))
            {
                return Unauthorized("Invalid API Key or Secret");
            }

            if (_aiService == null)
            {
                return BadRequest("AI Service is not available");
            }

            var question = new MessageForCreation()
            {
                ThreadId = default,
                ChannelName = string.IsNullOrWhiteSpace(ChannelName) ? "Walter1" : ChannelName,
                Content = QuestionText,
                ContentType = "text/plain",
                MessageType = "question"
            };

            var aiResponse = await _aiService.SendMessage(question);

            return Ok(aiResponse);
        }

        // Helper method to validate API Key and Secret
        private bool ValidateApiKeyAndSecret(HttpRequest request)
        {
            // Retrieve static values from appsettings.json
            var expectedApiKey = _structuredDataOptions.ApiKey;
            var expectedApiSecret = _structuredDataOptions.ApiSecret;

            // Check the request headers
            if (!request.Headers.TryGetValue("x-api-key", out var providedApiKey) ||
                !request.Headers.TryGetValue("x-api-secret", out var providedApiSecret))
            {
                return false;
            }

            // Validate against expected values
            return providedApiKey == expectedApiKey && providedApiSecret == expectedApiSecret;
        }
    }
}
