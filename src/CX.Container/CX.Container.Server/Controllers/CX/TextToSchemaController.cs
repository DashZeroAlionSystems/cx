using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Assistants;
using CX.Engine.Assistants.TextToSchema;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.SharedOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Controllers.CX;
[Authorize]
[ApiController]
[Route("api/text-to-schema")]
[ApiVersion("1.0")]
public sealed class TextToSchemaController(IOptionsMonitor<StructuredDataOptions> structuredDataOptions, 
    ILogger<TextToSchemaController> logger, 
    ACLService aclService,
    IServiceProvider sp) : ControllerBase
{
    [HttpPost("{assistantName}")]
    [RequiresAtLeastUserRole]
    public async Task<ActionResult> ConvertToSchemaAsync(string assistantName, [FromForm] ConvertRequest request)
    {
        logger.LogInformation("Received request for assistant: {AssistantName}", assistantName);
        
        // Authorization handled by RequiresAtLeastUserRole attribute
        
        try
        {
            if (string.IsNullOrWhiteSpace(assistantName))
                return BadRequest(new { Message = "AssistantName must not be empty" });

            var assistant = sp.GetNamedService<TextToSchemaAssistant>(assistantName);
            if (assistant == null)
                return BadRequest(new { Message = $"Assistant '{assistantName}' not found" });

            AssistantAnswer result;
            Dictionary<string, string> parameters = new();
            
            foreach (var prop in Request.Form)
            {
                if (prop.Key != "File" && prop.Key != "Text")
                    parameters[prop.Key] = prop.Value;
            }

            var ext = request.File?.FileName != null ? Path.GetExtension(request.File.FileName) : "";

            // Handle PDFs
            if (ext == ".pdf")
            {
                using var ms = new MemoryStream();
                await request.File!.CopyToAsync(ms);
                var bytes = ms.ToArray();
                result = await assistant.FromPdfAsync(bytes, parameters);
            }
            //Handle DocX
            else if (ext == ".docx")
            {
                using var ms = new MemoryStream();
                await request.File!.CopyToAsync(ms);
                var bytes = ms.ToArray();
                result = await assistant.FromDocXAsync(bytes, parameters);
            }
            //Handle images
            else if (ext is ".jpg" or ".jpeg" or ".png")
            {
                using var ms = new MemoryStream();
                await request.File!.CopyToAsync(ms);
                var bytes = ms.ToArray();
                result = await assistant.FromImageAsync(bytes, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(ext))
            {
                request.Text = await new StreamReader(request.File!.OpenReadStream()).ReadToEndAsync();
                result = await assistant.FromTextAsync(request.Text, parameters);
            }
            // Handle text input
            else if (!string.IsNullOrWhiteSpace(request.Text))
            {
                result = await assistant.FromTextAsync(request.Text, parameters);
            }
            else
                return BadRequest(new { Message = "Either a compatible File form-data property or Text form-data property must be provided" });

            return Ok(new
            {
                Answer = result.Answer.TryParseToJsonElement(out var aje) ? aje : (object)result.Answer,
                IsRefusal = result.IsRefusal
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing request");
            return BadRequest(new { Message = ex.Message });
        }
    }

    public class ConvertRequest
    {
        public string Text { get; set; }
        public IFormFile File { get; set; }
    }
}