using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.SharedOptions;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Controllers.CX;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public sealed class StorageServiceController : ControllerBase
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<StorageServiceController> _logger;
    private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
    private readonly ACLService _aclService;

    public StorageServiceController(
        IServiceProvider sp,
        ILogger<StorageServiceController> logger,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _structuredDataOptions = structuredDataOptions ?? throw new ArgumentNullException(nameof(structuredDataOptions));
        _aclService = aclService ?? throw new ArgumentNullException(nameof(aclService));
    }

    [HttpGet("documents")]
    [RequiresAtLeastUserRole]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public async Task<IActionResult> GetDocument(
        [FromQuery] string id,
        [FromQuery] string name,
        [FromQuery] string store_provider,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("No 'id' query parameter.");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("No 'name' query parameter.");

            if (string.IsNullOrWhiteSpace(store_provider))
                return BadRequest("No 'store_provider' query parameter.");
            
            var store = _sp.GetRequiredNamedService<IStorageService>(store_provider);
            var content = await store.GetContentAsync(id);

            if (content == null)
                return NotFound();

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{name}\"";
            return File(content.Content, content.ContentType.GetContentType());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document with id {Id} from store {StoreProvider}", id, store_provider);
            return StatusCode(500, new { Message = "An error occurred while retrieving the document" });
        }
    }
} 

