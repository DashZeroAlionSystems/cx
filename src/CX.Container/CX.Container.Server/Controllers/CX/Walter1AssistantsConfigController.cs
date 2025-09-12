using System.Text.Json;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Assistants.Walter1;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.SharedOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static CX.Container.Server.Extensions.Services.CXConsts;

namespace CX.Container.Server.Controllers.CX
{
    [Authorize]
    [ApiController]
    [Route("api/config/walter1assistants")]
    [ApiVersion("1.0")]
    public sealed class Walter1AssistantsConfigController(
        IServiceProvider sp,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService) : ControllerBase
    {
        private readonly JsonStore configStore = new("config_walter1assistants", 100, pg_default, sp);

        [HttpGet]
        [RequiresAtLeastUserRole]
        [Route("")]
        public async Task<ActionResult<IEnumerable<JsonElement>>> GetConfigAsync()
        {
            // Authorization handled by RequiresAtLeastUserRole attribute
            return Ok((await configStore.GetAllAsync()).Select(r => new {
                Key = r.Key,
                Value = r.Value.TryParseToJsonElement(out var je) ? je : (object)r.Value
            }));
        }

        [HttpGet]
        [RequiresAtLeastUserRole]
        [Route("{id}")]
        public async Task<ActionResult<JsonElement>> GetConfigByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { Message = $"{nameof(id)} must not be empty" });

            var config = await configStore.GetAsync<JsonElement>(id);

            if (config.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return NotFound();

            return Ok(config);
        }

        [HttpPut]
        [RequiresAtLeastUserRole]
        [Route("{id}")]
        public async Task<ActionResult<Walter1AssistantOptions>> CreateConfigAsync(string id, [FromBody] Walter1AssistantOptions config)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { Message = $"{nameof(id)} must not be empty" });

            await configStore.SetAsync(id, config);
            return NoContent();
        }

        [HttpDelete]
        [RequiresAtLeastUserRole]
        [Route("{id}")]
        public async Task<ActionResult> DeleteConfigAsync(string id)
        {
            await configStore.DeleteAsync(id);
            return NoContent();
        }
    }
}