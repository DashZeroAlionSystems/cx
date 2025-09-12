using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Archives.Pinecone;
using CX.Engine.TextProcessors.Splitters;
using CX.Engine.Common.ACL;
using CX.Engine.SharedOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Controllers.CX;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PineconeArchiveController : ControllerBase
{
    private readonly PineconeChunkArchive _archive;
    private readonly ILogger<PineconeArchiveController> _logger;
    private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
    private readonly ACLService _aclService;

    public PineconeArchiveController(
        PineconeChunkArchive archive, 
        ILogger<PineconeArchiveController> logger,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService)
    {
        _archive = archive;
        _logger = logger;
        _structuredDataOptions = structuredDataOptions;
        _aclService = aclService;
    }

    [HttpPost("register")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> RegisterChunks([FromBody] ChunkRegistrationRequest request)
    {
        // Authorization handled by RequiresAtLeastUserRole attribute
        
        try
        {
            await _archive.RegisterAsync(request.DocumentId, request.Chunks, request.Namespace);
            return Ok(new { message = $"Successfully registered {request.Chunks.Count} chunks" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering chunks");
            return StatusCode(500, new { error = "Failed to register chunks", details = ex.Message });
        }
    }

    [HttpPost("register/single")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> RegisterSingleChunk([FromBody] SingleChunkRequest request)
    {
        // Authorization handled by RequiresAtLeastUserRole attribute
        
        try
        {
            await _archive.RegisterAsync(request.Chunk, request.Namespace);
            return Ok(new { message = "Successfully registered chunk" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering single chunk");
            return StatusCode(500, new { error = "Failed to register chunk", details = ex.Message });
        }
    }

    [HttpDelete("document/{documentId}")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> RemoveDocument(Guid documentId)
    {
        // Authorization handled by RequiresAtLeastUserRole attribute
        
        try
        {
            await _archive.RemoveDocumentAsync(documentId);
            return Ok(new { message = $"Successfully removed document {documentId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document");
            return StatusCode(500, new { error = "Failed to remove document", details = ex.Message });
        }
    }

    [HttpDelete("clear")]
    [RequiresAtLeastUserRole]
    public async Task<IActionResult> Clear([FromQuery] string ns = null)
    {
        // Authorization handled by RequiresAtLeastUserRole attribute
        
        try
        {
            await _archive.ClearAsync(ns);
            return Ok(new { message = "Successfully cleared archive" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing archive");
            return StatusCode(500, new { error = "Failed to clear archive", details = ex.Message });
        }
    }
}

public class ChunkRegistrationRequest
{
    public Guid DocumentId { get; set; }
    public List<TextChunk> Chunks { get; set; }
    public string Namespace { get; set; }
}

public class SingleChunkRequest
{
    public TextChunk Chunk { get; set; }
    public string Namespace { get; set; }
}