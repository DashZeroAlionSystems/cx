using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using CX.Engine.Assistants;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using Microsoft.AspNetCore.Authorization;

namespace CX.Container.Server.Controllers.CX;

[Authorize]
[Route("api/assistant")]
[ApiController]
public class AssistantController : ControllerBase
{
   private readonly IServiceProvider _serviceProvider;
   private readonly ILogger<AssistantController> _logger;

   public class AskRequest
   {
       [Required]
       public string Question { get; set; }

       [Required]
       public string AssistantName { get; set; }
   }

   public AssistantController(IServiceProvider serviceProvider, ILogger<AssistantController> logger)
   {
       _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
       _logger = logger ?? throw new ArgumentNullException(nameof(logger));
   }

   [HttpPost("ask")]
   [Consumes("application/json")]
   [Produces("application/json")]
   public async Task<JsonResult> Ask([FromBody] AskRequest request)
   {
       try
       {
           var assistant = _serviceProvider.GetRequiredNamedService<IAssistant>(request.AssistantName);
           var result = await assistant.AskAsync(request.Question, new AgentRequest());

           try
           {
               // First try to parse as JSON
               var jsonResponse = JsonSerializer.Deserialize<JsonDocument>(result.Answer);
               var root = jsonResponse.RootElement;

               // Check if it's a table response in content property
               if (root.TryGetProperty("content", out var content))
               {
                   var contentStr = content.GetString();
                   if (contentStr?.Contains("|") == true)
                   {
                       var specs = ParseTableContent(contentStr);
                       return new JsonResult(new
                       {
                           success = true,
                           specifications = specs,
                           timestamp = DateTime.UtcNow
                       });
                   }
               }

               // Check if it's a translation response
               if (result.Answer.Contains("**") && result.Answer.Contains("-"))
               {
                   var translations = ParseTranslations(result.Answer);
                   return new JsonResult(new
                   {
                       success = true,
                       translations = translations,
                       timestamp = DateTime.UtcNow
                   });
               }

               // Return original JSON for other types
               return new JsonResult(new
               {
                   success = true,
                   data = JsonSerializer.Deserialize<object>(result.Answer),
                   timestamp = DateTime.UtcNow
               });
           }
           catch (JsonException)
           {
               // Handle plain text responses with table content
               if (result.Answer.Contains("|"))
               {
                   var specs = ParseTableContent(result.Answer);
                   return new JsonResult(new
                   {
                       success = true,
                       specifications = specs,
                       timestamp = DateTime.UtcNow
                   });
               }

               // Handle translation responses
               if (result.Answer.Contains("**") && result.Answer.Contains("-"))
               {
                   var translations = ParseTranslations(result.Answer);
                   return new JsonResult(new
                   {
                       success = true,
                       translations = translations,
                       timestamp = DateTime.UtcNow
                   });
               }

               // Default plain text response
               return new JsonResult(new
               {
                   success = true,
                   content = result.Answer,
                   timestamp = DateTime.UtcNow
               });
           }
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error processing ask request");
           return new JsonResult(new
           {
               success = false,
               error = "An error occurred while processing your request",
               timestamp = DateTime.UtcNow
           })
           { StatusCode = 500 };
       }
   }

   private Dictionary<string, string> ParseTableContent(string contentStr)
   {
       var specs = new Dictionary<string, string>();
       var pageLines = contentStr.Split(new[] { "--- PAGE" }, StringSplitOptions.RemoveEmptyEntries);
       
       foreach (var page in pageLines)
       {
           var lines = page.Split('\n')
               .Where(line => 
                   line.Contains("|") && 
                   !line.Contains(":-") && 
                   !line.StartsWith("| Specification") &&
                   !line.StartsWith("| Value"))
               .Select(line => line.Trim());

           foreach (var line in lines)
           {
               var parts = line.Split('|')
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Select(p => p.Trim())
                   .ToList();

               if (parts.Count >= 2)
               {
                   var key = parts[0].TrimEnd(':').Trim();
                   var value = string.Join(" ", parts.Skip(1)).Trim();
                   if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                   {
                       specs[key] = value;
                   }
               }
           }
       }

       return specs;
   }

   private Dictionary<string, string> ParseTranslations(string content)
   {
       var translations = new Dictionary<string, string>();
       var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
           .Where(s => s.Contains("**") && s.Contains("-"));

       foreach (var line in lines)
       {
           var match = Regex.Match(line, @"\*\*(.*?)\*\*\s*-\s*(.*)");
           if (match.Success)
           {
               var key = match.Groups[1].Value.Trim();
               var value = match.Groups[2].Value.Trim();
               translations[key.ToLower()] = value.ToLower();
           }
       }

       return translations;
   }
}