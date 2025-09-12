using System.ComponentModel.DataAnnotations;
using CX.Engine.CallAnalysis;
using CX.Engine.Common;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aela.Server.Controllers.CX;

[ApiController]
[Authorize]
[Route("api/call-analyzer")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class CallAnalyzerController : ControllerBase
{
    private readonly ILogger<CallAnalyzerController> _logger;
    private readonly IServiceProvider _sp;

    public CallAnalyzerController(ILogger<CallAnalyzerController> logger, [NotNull] IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    [HttpPost("{id}")]
    [ProducesResponseType(typeof(CallAnalyzerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CallAnalyzerResult>> AnalyzeAsync(
        string id,
        [Required(ErrorMessage = "Please provide an audio file")]
        [FromForm(Name = "file")]
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is missing or empty.");
        }

        var allowedMimeTypes = new HashSet<string>
        {
            // WAV Variants
            "audio/wav",
            "audio/wave",  // Some browsers and tools use this
            "audio/x-wav", // Older formats may use this

            // MP3 Variants
            "audio/mpeg",  // Standard MP3
            "audio/mp3",   // Less common, but some tools use it

            // OGG Variants
            "audio/ogg",   // Standard OGG format
            "application/ogg", // Some servers send this instead of audio/ogg

            // M4A Variants
            "audio/x-m4a", // Some legacy software
            "audio/mp4",   // Often used for M4A
            "audio/aac"    // M4A files sometimes show as AAC
        };

        if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest($"Invalid file type: {file.ContentType}. Only WAV, MP3, OGG, and M4A are allowed.");
        }

        CallAnalyzer callAnalyzer;

        try
        {
            callAnalyzer = _sp.GetRequiredNamedService<CallAnalyzer>(id);
        }
        catch (InvalidOperationException)
        {
            return BadRequest($"Call analyzer with id {id} does not exist ");
        }

        var result = await callAnalyzer.AnalyzeAsync(file.OpenReadStream(), file.FileName);
        return Ok(result);
    }
}