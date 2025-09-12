using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Configuration;
using CX.Engine.DocExtractors;
using CX.Engine.SharedOptions;

namespace CX.Container.Server.Controllers.CX
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DocXToPDFController : ControllerBase
    {
        private readonly DocXToPDF _converter;
        private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
        private readonly ACLService _aclService;

        public DocXToPDFController(
            DocXToPDF converter,
            IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
            ACLService aclService)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _structuredDataOptions = structuredDataOptions ?? throw new ArgumentNullException(nameof(structuredDataOptions));
            _aclService = aclService ?? throw new ArgumentNullException(nameof(aclService));
        }

        /// <summary>
        /// Converts a DOCX file to PDF format
        /// </summary>
        /// <param name="file">The DOCX file to convert</param>
        /// <returns>The converted PDF file</returns>
        /// <response code="200">Returns the converted PDF file</response>
        /// <response code="400">If the file is invalid or empty</response>
        /// <response code="415">If the file is not a DOCX file</response>
        [HttpPost("convert")]
        [RequiresAtLeastUserRole]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> ConvertToPDF(IFormFile file)
        {
            // Authorization handled by RequiresAtLeastUserRole attribute

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, "Only DOCX files are supported");

            try
            {
                using var stream = file.OpenReadStream();
                var pdfBytes = await _converter.ConvertToPDFAsync(stream);

                return File(
                    pdfBytes,
                    "application/pdf",
                    Path.ChangeExtension(file.FileName, ".pdf")
                );
            }
            catch (Exception ex)
            {
                // Log the exception details here if needed
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"Error converting file: {ex.Message}"
                );
            }
        }
    }
}