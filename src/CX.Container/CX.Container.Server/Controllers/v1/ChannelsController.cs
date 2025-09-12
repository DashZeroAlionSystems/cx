using CX.Container.Server.Domain;
using CX.Container.Server.Exceptions;
using CX.Engine.Common;
using CX.Engine.Common.Db;
using CX.Engine.Common.PostgreSQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CX.Container.Server.Controllers.v1;

[ApiController]
[Route("api/channels")]
[ApiVersion("1.0")]

public sealed class ChannelsController : ControllerBase
{
    private readonly PostgreSQLClient _sql;

    public ChannelsController(IServiceProvider sp)
    {
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>("pg_default");
    }

    public class ChannelModel
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Gets a single page image by Document Id and Page No.
    /// </summary>
    [HttpGet("", Name = "GetChannels")]
    [Authorize]
    public async Task<ActionResult> GetChannels()
    {
        return new OkObjectResult(await _sql.ListAsync("SELECT key, (value->>'DisplayName')::text AS displayname FROM config_channels WHERE (value->>'ShowInUI')::boolean = true OR key = 'ui'",
            row => new ChannelModel
            {
                Key = row.Get<string>("key"),
                DisplayName = row.GetNullable<string>("displayname") ?? (row.Get<string>("key") == "ui" ? "Default Channel" : row.Get<string>("key"))
            }));
    }

    [HttpPut("{key}/display-name", Name = "SetChannelName")]
    [Authorize] // Basic authorization check
    public async Task<ActionResult> SetChannelName(string key, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return new BadRequestObjectResult("Display name cannot be empty.");
        
        if (displayName.Length > 30)
            return new BadRequestObjectResult("Display name cannot be longer than 30 characters.");
        
        await _sql.ExecuteAsync($$"""
UPDATE config_channels SET value = jsonb_set(value, '{DisplayName}', {{JsonSerializer.Serialize(displayName)}}::jsonb) WHERE key = {{key}}
""");
        return new OkResult();
    }
}