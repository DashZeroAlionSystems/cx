using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CX.Engine.Common.Meta;
using CX.Engine.DocExtractors.Text;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/pdf")]
public class PDFExtractionController : ControllerBase
{
    private readonly PDFPlumber _pdfPlumber;
    private readonly ILogger<PDFExtractionController> _logger;

    public class ExtractTextResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public Dictionary<string, Dictionary<string, string>> FormattedContent { get; set; }
        public List<string> Errors { get; set; }
        public int? Pages { get; set; }
    }

    public PDFExtractionController(PDFPlumber pdfPlumber, ILogger<PDFExtractionController> logger)
    {
        _pdfPlumber = pdfPlumber ?? throw new ArgumentNullException(nameof(pdfPlumber));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("extract")]
    public async Task<ActionResult<ExtractTextResponse>> ExtractText(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        try
        {
            using var stream = file.OpenReadStream();
            var meta = new DocumentMeta();
            
            var content = await _pdfPlumber.ExtractToTextAsync(stream, meta);

            return Ok(new ExtractTextResponse
            {
                Success = true,
                Content = content,
                FormattedContent = ParseContentToJson(content),
                Pages = meta.Pages,
                Errors = meta.ExtractionErrors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            return StatusCode(500, new ExtractTextResponse
            {
                Success = false,
                Errors = new List<string> { "Error processing PDF: " + ex.Message }
            });
        }
    }

    private Dictionary<string, Dictionary<string, string>> ParseContentToJson(string content)
    {
        var pageData = new Dictionary<string, Dictionary<string, string>>();
        var currentPage = 1;
        var currentPageData = new Dictionary<string, string>();

        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Check for page marker
            var pageMatch = Regex.Match(line, @"--- PAGE (\d+) ---");
            if (pageMatch.Success)
            {
                // Save previous page data if exists
                if (currentPageData.Count > 0)
                {
                    pageData[$"Page {currentPage}"] = currentPageData;
                }

                // Start new page
                currentPage = int.Parse(pageMatch.Groups[1].Value);
                currentPageData = new Dictionary<string, string>();
                continue;
            }

            // Process table rows
            if (line.Contains("|"))
            {
                var trimmedLine = line.Trim();
                var columns = trimmedLine.Split('|')
                    .Select(col => col.Trim())
                    .Where(col => !string.IsNullOrEmpty(col))
                    .ToList();

                // Skip separator rows and empty rows
                if (columns.Count < 2 || 
                    columns[0].Contains("---") || 
                    string.IsNullOrWhiteSpace(columns[0]))
                {
                    continue;
                }

                // Clean and process the key
                string key = Regex.Replace(columns[0], @"\s+", " ").Trim();
                string value = Regex.Replace(columns[1], @"\s+", " ").Trim();

                // Remove rows with separators or empty values
                if (!string.IsNullOrEmpty(key) && 
                    !key.Contains("---") && 
                    !string.IsNullOrEmpty(value))
                {
                    // Handle potential duplicate keys
                    if (currentPageData.ContainsKey(key))
                    {
                        currentPageData[key] += " " + value;
                    }
                    else
                    {
                        currentPageData[key] = value;
                    }
                }
            }
        }

        // Add last page
        if (currentPageData.Count > 0)
        {
            pageData[$"Page {currentPage}"] = currentPageData;
        }

        return pageData;
    }

    // Configure JSON serialization options
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}