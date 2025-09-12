using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Text.Json;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Stores.Binary.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.SharedOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using static CX.Container.Server.Extensions.Services.CXConsts;

namespace CX.Container.Server.Controllers.CX;
[Authorize]
[ApiController]
[Route("api/postgresql")]
[ApiVersion("1.0")]
public class PostgreSQLController : ControllerBase 
{
    private readonly PostgreSQLClient _sqlClient;
    private readonly ILogger<PostgreSQLController> _logger;
    private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
    private readonly ACLService _aclService;

    public PostgreSQLController(
        IServiceProvider sp,
        ILogger<PostgreSQLController> logger,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService)
    {
        _sqlClient = sp.GetRequiredNamedService<PostgreSQLClient>("pg_default");
        _logger = logger;
        _structuredDataOptions = structuredDataOptions;
        _aclService = aclService;
    }

    [HttpGet("table/{tableName}")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> GetTableData([FromRoute][Required] string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return BadRequest(new { Message = "Table name is required" });
        }

        try
        {
            // Initialize with a size estimation
            var handler = new NpgsqlCommandInterpolatedStringHandler(20, 1);
            handler.AppendLiteral("SELECT * FROM ");
            handler.AppendFormatted(new InjectRaw(tableName));

            using var command = handler.GetCommand();
            var results = await _sqlClient.ListAsync(command, MapResultRow);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from table {TableName}", tableName);
            return StatusCode(500, new { error = "Failed to fetch table data" });
        }
    }

    [HttpPost("table/{tableName}")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> UpdateTable([FromRoute][Required] string tableName, [FromBody] TableUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return BadRequest(new { Message = "Table name is required" });
        }

        try
        {
            var handler = new NpgsqlCommandInterpolatedStringHandler(40, 3);
            handler.AppendLiteral("UPDATE ");
            handler.AppendFormatted(new InjectRaw(tableName));
            handler.AppendLiteral(" SET ");
            handler.AppendFormatted(new InjectRaw(request.SetClause));
            handler.AppendLiteral(" WHERE ");
            handler.AppendFormatted(new InjectRaw(request.WhereClause));
            
            using var command = handler.GetCommand();
            await _sqlClient.ExecuteAsync(command);
            return Ok(new { message = "Table updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table {TableName}", tableName);
            return StatusCode(500, new { error = "Failed to update table" });
        }
    }

    [HttpGet("query")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> ExecuteQuery([FromQuery][Required] string query)
    {
        try
        {
            var handler = new NpgsqlCommandInterpolatedStringHandler(query.Length, 1);
            handler.AppendFormatted(new InjectRaw(query));
            
            using var command = handler.GetCommand();
            var results = await _sqlClient.ListAsync(command, MapResultRow);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
            return StatusCode(500, new { error = "Query execution failed" });
        }
    }

    [HttpPost("execute")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> ExecuteCommand([FromBody] SqlCommandRequest request)
    { 
        try
        {
            var handler = new NpgsqlCommandInterpolatedStringHandler(request.Command.Length, 1);
            handler.AppendFormatted(new InjectRaw(request.Command));
            
            using var command = handler.GetCommand();
            await _sqlClient.ExecuteAsync(command);
            return Ok(new { message = "Command executed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command");
            return StatusCode(500, new { error = "Command execution failed" });
        }
    }

    private static Dictionary<string, object> MapResultRow(DbDataReader reader)
    {
        var result = new Dictionary<string, object>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            result[reader.GetName(i)] = value;
        }
        return result;
    }
}

public class SqlCommandRequest
{
    [Required]
    public string Command { get; set; }
}

public class TableUpdateRequest
{
    [Required]
    public string SetClause { get; set; }

    [Required]
    public string WhereClause { get; set; }
}