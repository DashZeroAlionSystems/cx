using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Aela.Server.Converters;
using CX.Container.Server.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.Meta;
using CX.Container.Server.Extensions.Services;
using CX.Engine.DocExtractors;
using Microsoft.AspNetCore.Authorization;

namespace CX.Container.Server.Controllers.CX
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MarkdownConverterController : ControllerBase
    {
        private readonly AnythingToMarkdownExtractor _extractor;
        private readonly string[] _allowedPdfExtensions = { ".pdf" };
        private readonly string[] _allowedPowerPointExtensions = { ".ppt", ".pptx" };
        private readonly string[] _allowedWordExtensions = { ".doc", ".docx" };
        private readonly string[] _allowedExcelExtensions = { ".xls", ".xlsx", ".csv" };
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        private readonly string[] _allowedTextExtensions = { ".txt", ".csv", ".json", ".xml" };
        private readonly string[] _allowedArchiveExtensions = { ".zip" };
        private readonly string[] _allowedHtmlExtensions = { ".html", ".htm" };

        public MarkdownConverterController(AnythingToMarkdownExtractor extractor)
        {
            _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        }

        private async Task<IActionResult> ProcessFile(IFormFile file, string[] allowedExtensions, string fileType)
        {
            if (file == null || file.Length == 0)
                return BadRequest($"No {fileType} file uploaded or file is empty");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest($"Invalid {fileType} file format. Allowed formats: {string.Join(", ", allowedExtensions)}");

            try
            {
                using var stream = file.OpenReadStream();
                var meta = new DocumentMeta();
                var markdownContent = await _extractor.ConvertToMarkdownAsync(stream, meta);

                Response.Headers.Add("Content-Disposition",
                    $"attachment; filename={Path.GetFileNameWithoutExtension(file.FileName)}.md");

                return Content(markdownContent, "text/markdown; charset=utf-8");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred while converting the {fileType} file: {ex.Message}");
            }
        }
        [RequiresAtLeastUserRole]
        [HttpPost("convert/pdf")]
        public async Task<IActionResult> ConvertPdfToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedPdfExtensions, "PDF");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/powerpoint")]
        public async Task<IActionResult> ConvertPowerPointToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedPowerPointExtensions, "PowerPoint");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/word")]
        public async Task<IActionResult> ConvertWordToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedWordExtensions, "Word");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/excel")]
        public async Task<IActionResult> ConvertExcelToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedExcelExtensions, "Excel");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/image")]
        public async Task<IActionResult> ConvertImageToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedImageExtensions, "Image");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/text")]
        public async Task<IActionResult> ConvertTextToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedTextExtensions, "Text");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/archive")]
        public async Task<IActionResult> ConvertArchiveToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedArchiveExtensions, "Archive");

        [RequiresAtLeastUserRole]
        [HttpPost("convert/html")]
        public async Task<IActionResult> ConvertHtmlToMarkdown(IFormFile file)
            => await ProcessFile(file, _allowedHtmlExtensions, "HTML");

        [RequiresAtLeastUserRole]
        [HttpPost("convert")]
        public async Task<IActionResult> ConvertToMarkdown(IFormFile file)
        {
            var allAllowedExtensions = _allowedPdfExtensions
                .Concat(_allowedPowerPointExtensions)
                .Concat(_allowedWordExtensions)
                .Concat(_allowedExcelExtensions)
                .Concat(_allowedImageExtensions)
                .Concat(_allowedTextExtensions)
                .Concat(_allowedArchiveExtensions)
                .Concat(_allowedHtmlExtensions)
                .ToArray();

            return await ProcessFile(file, allAllowedExtensions, "file");
        }
    }
}