using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Importing;
using CX.Engine.SharedOptions;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using CX.Container.Server.Common;
using static CX.Container.Server.Extensions.Services.CXConsts;

namespace CX.Container.Server.Controllers.CX
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VectorLinkImporterController : ControllerBase
    {
        private readonly VectorLinkImporter _importer;
        private readonly ILogger<VectorLinkImporterController> _logger;
        private readonly LangfuseService _langfuseService;
        private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
        private readonly ACLService _aclService;
        private readonly ActivitySource _activitySource = new("CX.Container");

        public VectorLinkImporterController(
            VectorLinkImporter importer,
            ILogger<VectorLinkImporterController> logger,
            LangfuseService langfuseService,
            IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
            ACLService aclService,
            ActivitySource activitySource)
        {
            _importer = importer ?? throw new ArgumentNullException(nameof(importer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
            _structuredDataOptions = structuredDataOptions ?? throw new ArgumentNullException(nameof(structuredDataOptions));
            _aclService = aclService ?? throw new ArgumentNullException(nameof(aclService));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        }

        [HttpPost("import")]
        [RequiresAtLeastUserRole]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> ImportAsync(
            [FromForm] IFormFile file,
            [FromForm] string? description = null,
            [FromForm] Guid? documentId = null,
            [FromForm] List<IFormFile>? attachments = null,
            [FromForm] HashSet<string>? tags = null,
            [FromForm] string? sourceDocumentDisplayName = null,
            [FromForm] string[]? channels = null,
            [FromForm] bool? extractImages = null,
            [FromForm] bool? preferImageTextExtraction = null,
            [FromForm] bool? trainCitations = null,
            [FromForm] bool? attachToSelf = null,
            [FromForm] bool? attachPageImages = null,
            [FromForm] string? archive = null)
        {
            using var activity = _activitySource.StartActivity("VectorLinkImporter.Import");
            var requestId = Guid.NewGuid();
            var finalDocumentId = documentId ?? Guid.NewGuid();
            
            activity?.SetTag("fileName", file?.FileName);
            activity?.SetTag("fileSize", file?.Length);
            activity?.SetTag("requestId", requestId.ToString());
            activity?.SetTag("documentId", finalDocumentId.ToString());

            _logger.LogInformation("Starting import process - RequestId: {RequestId}, DocumentId: {DocumentId}, FileName: {FileName}, FileSize: {FileSize}", 
                requestId, finalDocumentId, file?.FileName, file?.Length);

            // Authorization handled by RequiresAtLeastUserRole attribute

            var tempFilePath = string.Empty;
            var attachmentTempPaths = new List<string>();

            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Import request rejected - no file uploaded. RequestId: {RequestId}", requestId);
                    return BadRequest("No file uploaded");
                }

                _logger.LogInformation("Validating and preparing file for import - RequestId: {RequestId}", requestId);

                // Create a unique temporary file path using the uploaded file
                tempFilePath = Path.Combine(
                    Path.GetTempPath(), 
                    $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}"
                );

                _logger.LogDebug("Created temporary file path: {TempFilePath} - RequestId: {RequestId}", tempFilePath, requestId);

                // Create VectorLinkImportJob instance
                var job = new VectorLinkImportJob
                {
                    Description = description,
                    DocumentId = finalDocumentId,
                    Attachments = new List<AttachmentInfo>(),
                    Tags = tags ?? new HashSet<string>(),
                    SourceDocumentDisplayName = sourceDocumentDisplayName ?? file.FileName,
                    ExtractImages = extractImages,
                    PreferImageTextExtraction = preferImageTextExtraction,
                    TrainCitations = trainCitations,
                    AttachToSelf = attachToSelf,
                    AttachPageImages = attachPageImages,
                    Archive = archive
                };

                _logger.LogInformation("Created import job - RequestId: {RequestId}, ExtractImages: {ExtractImages}, TrainCitations: {TrainCitations}", 
                    requestId, job.ExtractImages, job.TrainCitations);

                // Save uploaded file
                _logger.LogDebug("Saving uploaded file to temporary location - RequestId: {RequestId}", requestId);
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                    fileStream.Position = 0; // Reset position to beginning before reading
                    job.DocumentContent = await fileStream.CopyToMemoryStreamAsync();
                }

                _logger.LogInformation("Successfully saved main document file - RequestId: {RequestId}, TempPath: {TempFilePath}", 
                    requestId, tempFilePath);

                // Save attachments
                if (attachments != null && attachments.Count > 0)
                {
                    _logger.LogInformation("Processing {AttachmentCount} attachments - RequestId: {RequestId}", 
                        attachments.Count, requestId);
                    
                    foreach (var attachment in attachments)
                    {
                        var attachmentTempFilePath = Path.Combine(
                            Path.GetTempPath(), 
                            $"{Guid.NewGuid()}{Path.GetExtension(attachment.FileName)}"
                        );
                        attachmentTempPaths.Add(attachmentTempFilePath);

                        using (var attachmentStream = new FileStream(attachmentTempFilePath, FileMode.Create))
                        {
                            await attachment.CopyToAsync(attachmentStream);
                            attachmentStream.Position = 0; // Reset position to beginning before reading
                            var attachmentContent = await attachmentStream.CopyToMemoryStreamAsync();
                            
                            var attachmentInfo = new AttachmentInfo
                            {
                                FileName = attachment.FileName,
                                CitationId = Guid.NewGuid(),
                                DoGetContentStreamAsync = () => Task.FromResult<Stream>(new MemoryStream(attachmentContent.ToArray()))
                            };
                            job.Attachments.Add(attachmentInfo);
                        }
                        
                        _logger.LogDebug("Saved attachment file: {AttachmentFileName} - RequestId: {RequestId}", 
                            attachment.FileName, requestId);
                    }
                }

                _logger.LogInformation("Starting VectorLinkImporter.ImportAsync operation - RequestId: {RequestId}", requestId);
                
                // Wait for the import process to complete entirely
                await _importer.ImportAsync(job);
                
                _logger.LogInformation("VectorLinkImporter.ImportAsync completed successfully - RequestId: {RequestId}, TotalChunks: {TotalChunks}", 
                    requestId, _importer.TotalChunksImported);

                activity?.SetStatus(ActivityStatusCode.Ok);
                
                var response = new 
                { 
                    Message = "Import completed successfully",
                    DocumentId = job.DocumentId,
                    TotalChunks = _importer.TotalChunksImported,
                    FileName = file.FileName,
                    FilePath = tempFilePath,
                    Attachments = job.Attachments.Select(a => new { a.FileName }).ToList(),
                    Description = job.Description,
                    Tags = job.Tags,
                    ExtractImages = job.ExtractImages,
                    PreferImageTextExtraction = job.PreferImageTextExtraction,
                    TrainCitations = job.TrainCitations,
                    AttachToSelf = job.AttachToSelf,
                    AttachPageImages = job.AttachPageImages,
                    Archive = job.Archive,
                    RequestId = requestId
                };

                _logger.LogInformation("Import process completed successfully - RequestId: {RequestId}, DocumentId: {DocumentId}, TotalChunks: {TotalChunks}", 
                    requestId, job.DocumentId, _importer.TotalChunksImported);
                
                return Ok(response);
            }
            catch (ArgumentException argEx)
            {
                activity?.SetStatus(ActivityStatusCode.Error, argEx.Message);
                _logger.LogError(argEx, "Validation error during document import - RequestId: {RequestId}, DocumentId: {DocumentId}", 
                    requestId, finalDocumentId);
                return BadRequest(new { Error = argEx.Message, RequestId = requestId });
            }
            catch (InvalidOperationException invOpEx)
            {
                activity?.SetStatus(ActivityStatusCode.Error, invOpEx.Message);
                _logger.LogError(invOpEx, "Invalid operation during document import - RequestId: {RequestId}, DocumentId: {DocumentId}", 
                    requestId, finalDocumentId);
                return BadRequest(new { Error = invOpEx.Message, RequestId = requestId });
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Unexpected error during document import - RequestId: {RequestId}, DocumentId: {DocumentId}, ErrorType: {ErrorType}", 
                    requestId, finalDocumentId, ex.GetType().Name);
                
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { 
                        Error = "An error occurred during import",
                        Details = ex.Message,
                        RequestId = requestId,
                        ErrorType = ex.GetType().Name
                    }
                );
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                        _logger.LogDebug("Cleaned up temporary file: {TempFilePath} - RequestId: {RequestId}", 
                            tempFilePath, requestId);
                    }

                    foreach (var attachmentPath in attachmentTempPaths)
                    {
                        if (System.IO.File.Exists(attachmentPath))
                        {
                            System.IO.File.Delete(attachmentPath);
                            _logger.LogDebug("Cleaned up attachment temporary file: {AttachmentPath} - RequestId: {RequestId}",
                                attachmentPath, requestId);
                        }
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up temporary files - RequestId: {RequestId}", requestId);
                }
            }
        }

        [HttpDelete("{documentId}")]
        [RequiresAtLeastUserRole]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteAsync(Guid documentId)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            try
            {
                if (documentId == Guid.Empty)
                    return BadRequest("Invalid document ID");
                
                await _importer.DeleteAsync(documentId);
                
                return Ok(new 
                { 
                    Message = "Document deleted successfully",
                    DocumentId = documentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document deletion for document ID: {DocumentId}", documentId);
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}