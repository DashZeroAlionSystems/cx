using System.Net.Mime;
using Aela.Server.Domain;
using CX.Container.Server.Common;
using CX.Container.Server.Domain;
using CX.Container.Server.Extensions.Services;
using CX.Engine.Assistants;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.FileServices;
using CX.Engine.QAndA;
using CX.Engine.SharedOptions;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using PuppeteerSharp.Helpers;

namespace Aela.Server.Controllers.CX;

public static class TaskExtensions
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Task timed out after {timeout.TotalSeconds} seconds");
        }
    }
}
[Authorize]
[ApiController]
[Route("api/QA")]
[ApiVersion("1.0")]
public sealed class QAController : ControllerBase
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<QAController> _logger;
    private readonly ChatCache _chatCache;
    private readonly FileService _fileService;
    private readonly IOptionsMonitor<StructuredDataOptions> _structuredDataOptions;
    private readonly ACLService _aclService;
    private readonly DynamicSlimLock _concurrencyLock;
    private volatile int _itemsInQueue;
    private const int MAX_CONCURRENT_EVALUATIONS = 1;
    private const int TIMEOUT_MINUTES = 60;
    private const int PROGRESS_UPDATE_INTERVAL_SECONDS = 15;
    private const int MAX_RETRIES = 3;
    private const int MIN_DELAY_SECONDS = 2;
    private const int MAX_DELAY_SECONDS = 30;

    public QAController(
        IServiceProvider sp,
        ILogger<QAController> logger,
        ChatCache chatCache,
        FileService fileService,
        IOptionsMonitor<StructuredDataOptions> structuredDataOptions,
        ACLService aclService)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatCache = chatCache ?? throw new ArgumentNullException(nameof(chatCache));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _structuredDataOptions = structuredDataOptions ?? throw new ArgumentNullException(nameof(structuredDataOptions));
        _aclService = aclService ?? throw new ArgumentNullException(nameof(aclService));
        
        _concurrencyLock = new DynamicSlimLock(MAX_CONCURRENT_EVALUATIONS);
    }

    [HttpPost("eval")]
    [RequiresAtLeastUserRole]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EvalAsync(
        [FromForm] IFormFile file,
        [FromQuery] string assistantName)
    {
        try
        {
            // Validate request
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (string.IsNullOrWhiteSpace(assistantName))
                return BadRequest("Assistant name is required");

            // Get required services
            var assistant = _sp.GetRequiredNamedService<IAssistant>(assistantName);
            var agent = _sp.GetRequiredNamedService<IChatAgent>("OpenAI.GPT-4o-mini") as OpenAIChatAgent;
            
            if (agent == null)
                return BadRequest("Required OpenAI agent not found");

            // Load and validate Excel file
            using var stream = file.OpenReadStream();
            using var qaDoc = new QASession();
            qaDoc.LoadFromExcel(stream);

            var entriesWithoutCriteria = qaDoc.Entries.Where(e => !e.Criteria.Any()).ToList();
            if (entriesWithoutCriteria.Any())
            {
                var questions = string.Join(", ", entriesWithoutCriteria.Select(e => e.Question));
                return BadRequest($"Some entries have no evaluation criteria defined. Questions: {questions}");
            }

            _logger.LogInformation("Starting QA evaluation for {EntryCount} entries", qaDoc.Entries.Count);
            
            // Evaluate QA entries with progress tracking
            var baseReq = new AgentRequest();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(TIMEOUT_MINUTES));
            
            // Create a task to monitor progress
            var progressTask = Task.Run(async () =>
            {
                var lastProgress = 0.0;
                while (!cts.Token.IsCancellationRequested)
                {
                    var completed = qaDoc.CompletedEntries;
                    var total = qaDoc.Entries.Count;
                    var progress = (double)completed / total;
                    
                    // Only log if progress has changed significantly
                    if (Math.Abs(progress - lastProgress) >= 0.05) // Log every 5% change
                    {
                        _logger.LogInformation("QA Evaluation Progress: {Progress:P2} ({Completed}/{Total})", 
                            progress, completed, total);
                        lastProgress = progress;
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(PROGRESS_UPDATE_INTERVAL_SECONDS), cts.Token);
                }
            }, cts.Token);

            try
            {
                // Track items in queue and acquire concurrency lock
                var itemsInQueue = Interlocked.Increment(ref _itemsInQueue);
                _logger.LogInformation("QA evaluation request queued. Current queue size: {QueueSize}", itemsInQueue);

                // Create a task to wait for the lock with cancellation
                var lockTask = Task.Run(async () =>
                {
                    await _concurrencyLock.WaitAsync();
                    Interlocked.Decrement(ref _itemsInQueue);
                    _logger.LogInformation("QA evaluation request processing started. Remaining queue size: {QueueSize}", _itemsInQueue);
                }, cts.Token);

                // Wait for either the lock to be acquired or cancellation
                await lockTask.WaitAsync(cts.Token);
                if (cts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("QA evaluation request cancelled while waiting for lock");
                    return BadRequest("Request cancelled while waiting for processing");
                }

                try
                {
                    // Run the evaluation with timeout
                    var score = await qaDoc.EvaluateAsync(assistant, _chatCache, agent, baseReq, _fileService, _sp)
                        .WithTimeout(TimeSpan.FromMinutes(TIMEOUT_MINUTES), cts.Token);

                    if (score.TotalCriteria == 0)
                    {
                        _logger.LogWarning("No criteria were evaluated during QA process");
                        return BadRequest("No criteria were evaluated. Please check the evaluation process.");
                    }

                    if (double.IsNaN(score.OverallPercentage) || double.IsInfinity(score.OverallPercentage))
                    {
                        _logger.LogError("Invalid score calculated: {Score}", score.OverallPercentage);
                        return BadRequest("Invalid score calculated. Score must be a valid number.");
                    }

                    _logger.LogInformation("QA evaluation completed successfully. Overall score: {Score:P2}", 
                        score.OverallPercentage);

                    // Save results
                    var resultStream = qaDoc.SaveToExcelStream();
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var sanitizedFileName = string.Join("_", originalFileName.Split(Path.GetInvalidFileNameChars()));
                    var filename = $"{sanitizedFileName}_evaluated_{timestamp}.xlsx";

                    Response.Headers.Add("Content-Disposition", new ContentDisposition
                    {
                        FileName = filename,
                        Inline = false
                    }.ToString());
                    
                    Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                    Response.Headers.Add("Pragma", "no-cache");
                    Response.Headers.Add("Expires", "0");

                    return File(
                        fileStream: resultStream,
                        contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileDownloadName: filename,
                        enableRangeProcessing: true
                    );
                }
                finally
                {
                    _concurrencyLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("QA evaluation timed out after {Minutes} minutes", TIMEOUT_MINUTES);
                return BadRequest($"QA evaluation timed out after {TIMEOUT_MINUTES} minutes. Progress: {qaDoc.CompletedEntries}/{qaDoc.Entries.Count} entries completed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing QA evaluation");
            return BadRequest($"Error processing file: {ex.Message}");
        }
    }
} 