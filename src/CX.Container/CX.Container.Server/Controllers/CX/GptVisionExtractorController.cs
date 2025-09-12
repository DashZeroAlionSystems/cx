using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.Meta;
using CX.Engine.Configuration;
using CX.Engine.DocExtractors;
using CX.Engine.DocExtractors.Text;
using CX.Engine.DocExtractors.Images;
using CX.Engine.SharedOptions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CX.Container.Server.Common;

namespace CX.Container.Server.Controllers.CX
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class Gpt4VisionExtractorController : ControllerBase
    {
        private readonly Gpt4VisionExtractor _extractor;
        private readonly DocImageExtraction _docImageExtraction;
        private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
        private readonly ACLService _aclService;
        private readonly ILogger<Gpt4VisionExtractorController> _logger;

        public Gpt4VisionExtractorController(
            Gpt4VisionExtractor extractor,
            DocImageExtraction docImageExtraction,
            IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
            ACLService aclService,
            ILogger<Gpt4VisionExtractorController> logger)
        {
            _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            _docImageExtraction = docImageExtraction ?? throw new ArgumentNullException(nameof(docImageExtraction));
            _structuredDataOptions = structuredDataOptions ?? throw new ArgumentNullException(nameof(structuredDataOptions));
            _aclService = aclService ?? throw new ArgumentNullException(nameof(aclService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string FormatExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Replace multiple newlines with two newlines
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Format bullet points consistently
            text = Regex.Replace(text, @"\n\s*-\s*", "\n• ");

            // Add space after periods if missing
            text = Regex.Replace(text, @"\.(?=[A-Z])", ". ");

            // Format phone numbers consistently
            text = Regex.Replace(text, @"TEL:\s*", "Tel: ");

            // Add extra line break before sections
            text = Regex.Replace(text, @"\n([A-Z][A-Z\s]+:)", "\n\n$1");

            // Format addresses more cleanly
            text = Regex.Replace(text, @"([A-Za-z]),\s*([A-Za-z])", "$1, $2");

            // Clean up any remaining multiple spaces
            text = Regex.Replace(text, @"\s+", " ");

            // Split into paragraphs
            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim());

            return string.Join("\n\n", paragraphs);
        }

        /// <summary>
        /// Extracts text from an image using GPT-4 Vision
        /// </summary>
        [HttpPost("extract")]
        [RequiresAtLeastUserRole]
        [ProducesResponseType(typeof(ExtractedTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> ExtractText(
            IFormFile file,
            [FromForm] DocumentMetaRequest? metadata = null)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded");
                return BadRequest("No file uploaded");
            }

            // Common image formats that GPT-4 Vision can process
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!supportedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Unsupported file type: {FileExtension}", fileExtension);
                return StatusCode(
                    StatusCodes.Status415UnsupportedMediaType,
                    $"Only {string.Join(", ", supportedExtensions)} files are supported"
                );
            }

            try
            {
                string extractedText;
                var meta = metadata?.ToDocumentMeta() ?? new DocumentMeta();

                if (fileExtension == ".pdf")
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    
                    var fileId = Guid.NewGuid();
                    var images = await _docImageExtraction.ExtractImagesAsync(fileId, memoryStream);
                    
                    if (images?.Any() == true)
                    {
                        var imageStore = _docImageExtraction.PDFToJpg.ImageStore;
                        extractedText = await _extractor.ExtractToTextAsync(file.FileName, memoryStream, meta, imageStore, images);
                        await _docImageExtraction.PDFToJpg.DeleteAsync(fileId);
                    }
                    else
                    {
                        memoryStream.Position = 0;
                        extractedText = await _extractor.ExtractToTextAsync(memoryStream, meta);
                    }
                }
                else
                {
                    using var stream = file.OpenReadStream();
                    extractedText = await _extractor.ExtractToTextAsync(stream, meta);
                }

                // Format the extracted text for better readability
                var formattedText = FormatExtractedText(extractedText);

                return Ok(new ExtractedTextResponse 
                { 
                    ExtractedText = formattedText,
                    RawText = extractedText,
                    FileType = fileExtension,
                    ProcessingTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Error = $"Error extracting text: {ex.Message}" }
                );
            }
        }
    }

    public class ExtractedTextResponse
    {
        /// <summary>
        /// The formatted, more readable version of the extracted text
        /// </summary>
        public string ExtractedText { get; set; } = string.Empty;

        /// <summary>
        /// The original, unformatted extracted text
        /// </summary>
        public string RawText { get; set; } = string.Empty;

        /// <summary>
        /// The type of file that was processed
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// When the processing was completed
        /// </summary>
        public DateTime ProcessingTime { get; set; }
    }

    /// <summary>
    /// Request model for document metadata
    /// </summary>
    public class DocumentMetaRequest
    {
        public Guid? Id { get; set; }
        public int? Pages { get; set; }
        public bool? ContainsTables { get; set; }
        public string? Description { get; set; }
        public string? SourceDocument { get; set; }
        public string? SourceDocumentGroup { get; set; }
        public string? SandboxUrl { get; set; }
        public string? Organization { get; set; }
        public string? ColumnHeaders { get; set; }
        public HashSet<string>? Tags { get; set; }
        public List<AttachmentInfo>? Attachments { get; set; }

        public DocumentMeta ToDocumentMeta() => new()
        {
            Id = Id,
            Pages = Pages,
            ContainsTables = ContainsTables,
            Description = Description ?? string.Empty,
            SourceDocument = SourceDocument ?? string.Empty,
            SourceDocumentGroup = SourceDocumentGroup ?? string.Empty,
            SandboxUrl = SandboxUrl ?? string.Empty,
            Organization = Organization ?? string.Empty,
            ColumnHeaders = ColumnHeaders ?? string.Empty,
            Tags = Tags ?? new(),
            Attachments = Attachments ?? new()
        };
    }
}