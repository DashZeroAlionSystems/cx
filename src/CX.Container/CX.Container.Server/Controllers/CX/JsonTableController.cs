using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Text.Json;
using CX.Container.Server.Common;
using Dapper;
using Microsoft.AspNetCore.Authorization;
[Authorize]
[ApiController]
[Route("api/jsontable/{tableName}")]
[ApiVersion("1.0")]
public sealed class JsonTableController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<JsonTableController> _logger;

    public JsonTableController(
        IConfiguration configuration,
        ILogger<JsonTableController> logger)
    {
        _connectionString = configuration.GetSection("PostgreSQLClient")
            ?.GetSection("default")
            ?.GetValue<string>("ConnectionString");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            const string error = "PostgreSQL connection string not found in configuration! Verify 'PostgreSQLClient:default:ConnectionString' is correctly configured.";
            logger.LogError(error);
            throw new InvalidOperationException(error);
        }
        _logger = logger;
    }

    private string SanitizeTableName(string tableName)
    {
        return new string(tableName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
    }

    [HttpGet]
    [RequiresAtLeastUserRole]
    public async Task<ActionResult<IEnumerable<dynamic>>> GetAllAsync(string tableName)
    {
        try
        {
            var safeTableName = SanitizeTableName(tableName);
            using var connection = new NpgsqlConnection(_connectionString);
            
            await connection.OpenAsync();
            
            // Verify table exists
            var tableExists = await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = @tableName)",
                new { tableName = safeTableName }
            );

            if (!tableExists)
            {
                _logger.LogWarning("Table {Table} does not exist", safeTableName);
                return NotFound(new { Message = $"Table {safeTableName} not found" });
            }

            var query = $"SELECT id as Key, data as Value FROM {safeTableName}";
            var results = await connection.QueryAsync(query);

            return Ok(results.Select(r => new {
                Key = r.Key,
                Value = r.Value != null ? JsonSerializer.Deserialize<JsonElement>(r.Value.ToString()) : null
            }));
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error getting records from {Table}: {Error}", tableName, ex.Message);
            return StatusCode(500, new { Message = $"Database error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting records from {Table}: {Error}", tableName, ex.Message);
            return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [RequiresAtLeastUserRole]
    public async Task<ActionResult<JsonElement>> GetByIdAsync(string tableName, string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { Message = $"{nameof(id)} must not be empty" });

            var safeTableName = SanitizeTableName(tableName);
            using var connection = new NpgsqlConnection(_connectionString);
            
            await connection.OpenAsync();

            // Verify table exists
            var tableExists = await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = @tableName)",
                new { tableName = safeTableName }
            );

            if (!tableExists)
            {
                _logger.LogWarning("Table {Table} does not exist", safeTableName);
                return NotFound(new { Message = $"Table {safeTableName} not found" });
            }
            
            var query = $"SELECT data as Value FROM {safeTableName} WHERE id = @Id";
            var result = await connection.QueryFirstOrDefaultAsync<string>(query, new { Id = id });

            if (result == null)
                return NotFound(new { Message = $"Record {id} not found" });

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(result);
            return Ok(jsonElement);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error getting record {Id} from {Table}: {Error}", id, tableName, ex.Message);
            return StatusCode(500, new { Message = $"Database error: {ex.Message}" });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for record {Id} from {Table}: {Error}", id, tableName, ex.Message);
            return StatusCode(500, new { Message = $"Invalid JSON data: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record {Id} from {Table}: {Error}", id, tableName, ex.Message);
            return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [RequiresAtLeastUserRole]
    public async Task<ActionResult> UpsertAsync(string tableName, string id, [FromBody] JsonElement data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { Message = $"{nameof(id)} must not be empty" });

            var safeTableName = SanitizeTableName(tableName);
            using var connection = new NpgsqlConnection(_connectionString);

            var query = $@"
                INSERT INTO {safeTableName} (key, value)
                VALUES (@Id, @Value::jsonb)
                ON CONFLICT (key) DO UPDATE 
                SET value = EXCLUDED.value
                RETURNING key";

            var result = await connection.ExecuteScalarAsync<string>(
                query,
                new { Id = id, Value = data.ToString() }
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting record {Id} in {Table}", id, tableName);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [RequiresAtLeastUserRole]
    public async Task<ActionResult> DeleteAsync(string tableName, string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { Message = $"{nameof(id)} must not be empty" });

            var safeTableName = SanitizeTableName(tableName);
            using var connection = new NpgsqlConnection(_connectionString);

            var query = $"DELETE FROM {safeTableName} WHERE key = @Id";
            await connection.ExecuteAsync(query, new { Id = id });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record {Id} from {Table}", id, tableName);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}
