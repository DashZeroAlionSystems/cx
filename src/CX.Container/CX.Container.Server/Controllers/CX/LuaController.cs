using System.Collections.Concurrent;
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
    [Authorize]
    [ApiController]
    [Route("api/lua")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class LuaController : ControllerBase
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<LuaController> _logger;
        private static readonly ConcurrentDictionary<string, (LuaCore Core, LuaInstance Instance)> _sessions = new();
        private readonly ACLService _aclService;
        private readonly StructuredDataOptions _options;

        public LuaController(
            IServiceProvider sp,
            ILogger<LuaController> logger,
            ACLService aclService,
            IOptionsMonitor<StructuredDataOptions> options)
        {
            _sp = sp;
            _logger = logger;
            _aclService = aclService;
            _options = options.CurrentValue;
        }

        [HttpGet("sessions")]
        [RequiresAtLeastUserRole]
        [ProducesResponseType(typeof(IEnumerable<ActiveSessionInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<IEnumerable<ActiveSessionInfo>> GetActiveSessions()
        {
            try
            {
                var activeSessions = _sessions.Select(kvp => new ActiveSessionInfo
                {
                    SessionId = kvp.Key,
                    CoreName = kvp.Value.Core.GetType().Name
                }).ToList();

                return Ok(activeSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active Lua sessions");
                return StatusCode(500, new { error = "Failed to retrieve sessions", details = ex.Message });
            }
        }
        
        [HttpPost("session")]
        [RequiresAtLeastUserRole]
        public ActionResult<string> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var luaCore = _sp.GetNamedService<LuaCore>(request.CoreName);
                if (luaCore == null)
                    return BadRequest(new { error = $"LuaCore '{request.CoreName}' not found" });

                var sessionId = Guid.NewGuid().ToString();
                var instance = luaCore.GetLuaInstance();
                _sessions[sessionId] = (luaCore, instance);

                return Ok(new { sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Lua session with core {CoreName}", request.CoreName);
                return StatusCode(500, new { error = "Failed to create session", details = ex.Message });
            }
        }

        [HttpPost("execute")]
        [RequiresAtLeastUserRole]
        public async Task<IActionResult> ExecuteCommand([FromBody] LuaCommandRequest request)
        {
            try
            {
                if (!_sessions.TryGetValue(request.SessionId, out var session))
                    return NotFound(new { error = "Session not found" });

                var result = await session.Core.RunAsync(request.Command, session.Instance);
                return Ok(new { result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Lua command in session {SessionId}", request.SessionId);
                return StatusCode(500, new { error = "Command execution failed", details = ex.Message });
            }
        }

        [HttpDelete("session/{sessionId}")]
        [RequiresAtLeastUserRole]
        public IActionResult CloseSession(string sessionId)
        {
            try
            {
                if (_sessions.TryRemove(sessionId, out var session))
                {
                    return Ok(new { message = "Session closed successfully" });
                }
            
                return NotFound(new { error = "Session not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Lua session");
                return StatusCode(500, new { error = "Failed to close session", details = ex.Message });
            }
        }
    }

    public class CreateSessionRequest
    {
        public string CoreName { get; set; }
    }

    public class LuaCommandRequest
    {
        public string SessionId { get; set; }
        public string Command { get; set; }
    }

    public class SessionInfo
    {
        public string SessionId { get; set; }
    }
    
    public class ActiveSessionInfo
    {
        public string SessionId { get; set; }
        public string CoreName { get; set; }
    }
}