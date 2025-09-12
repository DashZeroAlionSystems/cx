using System.Text.Json;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Assistants.Channels;
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
    [ApiController]
    [Route("api/config/channels")]
    [ApiVersion("1.0")]
    [Authorize]
    public sealed class ChannelsConfigController(
        IServiceProvider sp,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService) : ControllerBase
    {
        private readonly JsonStore configStore = new("config_channels", 100, pg_default, sp);

        [HttpGet]
        [RequiresAtLeastUserRole]
        [Route("")]
        public async Task<ActionResult<IEnumerable<ChannelOptions>>> GetConfigAsync()
        {
            // Role-based authorization handled by RequiresAtLeastUserRole attribute
            var configs = await configStore.GetAllAsync();
            return Ok(configs);  // Let ASP.NET Core handle the serialization
        }

        [HttpGet]
        [RequiresAtLeastUserRole]
        [Route("{id}")]
        public async Task<ActionResult<ChannelOptions>> GetConfigByIdAsync(string id)
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
        public async Task<ActionResult<ChannelOptions>> CreateConfigAsync(string id, [FromBody] ChannelOptions config)
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