using Flurl.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Container.Server.Configurations;
using CX.Container.Server.Databases;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocumentStatus;
using CX.Container.Server.Extensions.Application;
using CX.Engine.Archives;
using CX.Engine.Archives.Pinecone;
using CX.Engine.Common;
using CX.Engine.Importing;
using Microsoft.EntityFrameworkCore;

namespace Aela.Server.Wrappers
{
    public class AiServerRequestDto(string url)
    {
        public object Get()
        {
            return new
            {
                url = Url
            };
        }

        [JsonPropertyName("url")] public string Url { get; set; } = url;
    }

    public class AiServerResponseDto
    {
        [JsonPropertyName("task_id")] public string TaskId { get; set; }
    }

    public class AiResponse
    {
        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }
        public string Response { get; set; }
        public DateTime ActionDate { get; set; }
        public SourceDocumentStatus Status { get; set; }
    }

    public class AiServerTasks(
        IOptions<AiOptions> aiOptions,
        ILogger<AiServerTasks> logger,
        TimeProvider clock,
        VectorLinkImporter vectorLinkImporter,
        IServiceProvider sp) : IAiServerTasks
    {
        private readonly AiOptions _aiOptions = aiOptions.Value;

        public sealed record Response([property: JsonPropertyName("status")] string Status);

        public TimeProvider Clock { get; } = clock;

        public async Task<AiResponse> DecoratingTaskProgress(string DecoratorTaskID,
            CancellationToken cancellationToken)
        {
            try
            {
                var stringResponse = await _aiOptions.DecoratorUrlTaskId(DecoratorTaskID)
                    .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                    .WithHeader("x-api-key", _aiOptions.DocumentKey)
                    .GetAsync()
                    .ReceiveString();
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<Response>(stringResponse);

                    if (jsonResponse?.Status == "in progress")
                    {
                        return new AiResponse { IsSuccess = false, ErrorMessage = string.Empty };
                    }
                }
                catch (Exception ex)
                {
                    _ = ex.Message;
                }

                return new AiResponse
                {
                    IsSuccess = true,
                    Status = SourceDocumentStatus.DecoratingDone().Value,
                    Response = stringResponse,
                    ActionDate = Clock.GetLocalNow().Date
                };
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call.Response.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Decorating task progress");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        public Task<AiResponse> DeleteDecoratedData(Guid Id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<AiResponse> DeleteNameSpace(string pineconeNamespace, CancellationToken cancellationToken)
        {
            if (_aiOptions.UseVectorLinkImporter)
            {
                var archive = sp.GetRequiredNamedService<IArchive>("pinecone.default");

                if (archive is not PineconeChunkArchive pa)
                    throw new NotSupportedException("Only supported for Pinecone1Archives");

                var ss = pa.Snapshot;

                if (ss.Options.Namespace != pineconeNamespace)
                    throw new NotSupportedException(
                        $"Only supported for the namespace managed by the Pinecone1Archive: {ss.Options.Namespace} vs {pineconeNamespace}");

                await archive.ClearAsync();
                return new()
                {
                    IsSuccess = true,
                    Status = SourceDocumentStatus.Decorating().Value,
                    Response = "Archive cleared",
                    ActionDate = Clock.GetLocalNow().Date
                };
            }

            try
            {
                var stringResponse = await _aiOptions.DeleteNamespace()
                    .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                    .WithHeader("x-api-key", _aiOptions.TrainApiKey)
                    .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                    .WithHeader("NAMESPACE", pineconeNamespace)
                    .DeleteAsync(cancellationToken: cancellationToken)
                    .ReceiveString();

                if (stringResponse.Contains("deleted"))
                {
                    return new AiResponse
                    {
                        IsSuccess = true,
                        Status = SourceDocumentStatus.Decorating().Value,
                        Response = stringResponse,
                        ActionDate = Clock.GetLocalNow().Date
                    };
                }

                return new AiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = stringResponse,
                    Status = SourceDocumentStatus.Error().Value
                };
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call.Response.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting OCR task progress");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        public async Task<AiResponse> DeleteTrainedFile(Guid Id, string pineconeNamespace, bool forceOverride,
            CancellationToken cancellationToken)
        {
            if (_aiOptions.UseVectorLinkImporter)
            {
                await vectorLinkImporter.DeleteAsync(Id);

                return new AiResponse
                {
                    IsSuccess = true,
                    Status = SourceDocumentStatus.QueuedForRetrain().Value,
                    Response = "Document deleted",
                    ActionDate = Clock.GetLocalNow().Date
                };
            }

            try
            {
                if (pineconeNamespace == string.Empty)
                {
                    pineconeNamespace = _aiOptions.TrainNamespace;
                }

                var stringResponse = await _aiOptions.DeleteUrl
                    .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                    .WithHeader("x-api-key", _aiOptions.TrainApiKey)
                    .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                    .WithHeader("NAMESPACE", pineconeNamespace)
                    .WithHeader("document-id", Id)
                    .DeleteAsync(cancellationToken: cancellationToken)
                    .ReceiveString();

                if (stringResponse.Contains("Data deleted") || forceOverride)
                {
                    return new AiResponse
                    {
                        IsSuccess = true,
                        Status = SourceDocumentStatus.QueuedForRetrain().Value,
                        Response = stringResponse,
                        ActionDate = Clock.GetLocalNow().Date
                    };
                }

                return new AiResponse
                {
                    IsSuccess = false,
                    ErrorMessage = stringResponse,
                    Status = SourceDocumentStatus.Error().Value
                };
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call?.Response?.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call?.Response?.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call?.Response?.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting OCR task progress");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        public Task<AiResponse> GetLogsFromAI(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<AiResponse> OcrTaskProgress(string OCRTaskID, CancellationToken cancellationToken)
        {
            try
            {
                var stringResponse = await _aiOptions.ProcessDocumentStatusUrlTaskId(OCRTaskID)
                    .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                    .WithHeader("x-api-key", _aiOptions.DocumentKey)
                    .WithHeader("Content-Type", "application/json")
                    .GetAsync(cancellationToken: cancellationToken)
                    .ReceiveString();

                try
                {
                    if (!stringResponse.StartsWith("{"))
                    {
                        return new AiResponse
                        {
                            IsSuccess = true,
                            Status = SourceDocumentStatus.OCRDone().Value,
                            Response = stringResponse,
                            ActionDate = Clock.GetLocalNow().Date
                        };
                    }

                    var jsonResponse = JsonSerializer.Deserialize<Response>(stringResponse);

                    if (jsonResponse?.Status == "in progress")
                    {
                        return new AiResponse { IsSuccess = false, ErrorMessage = string.Empty };
                    }
                }
                catch (FlurlHttpException ex)
                {
                    _ = ex.Message;
                    throw;
                }
                catch (Exception ex)
                {
                    _ = ex.Message;
                }

                return new AiResponse
                {
                    IsSuccess = true,
                    Status = SourceDocumentStatus.OCRDone().Value,
                    Response = stringResponse,
                    ActionDate = Clock.GetLocalNow().Date
                };
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call.Response.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting OCR task progress");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        public Task<AiResponse> SetChatOptions(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AiResponse> SetDecoratingOptions(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AiResponse> SetPineconeOptions(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<AiResponse> StartDecoratingTask(SourceDocumentDto sourceDocument,
            CancellationToken cancellationToken)
        {
            try
            {
                if (sourceDocument.Name == null)
                {
                    throw new Exception("StartDecoratingTask: sourceDocument.Name is null");
                }

                if (sourceDocument.OCRText == null)
                {
                    throw new Exception("StartDecoratingTask: sourceDocument.OCRText is null");
                }

                var tagsData = sourceDocument.Tags.IsNotNullOrWhiteSpace() ? sourceDocument.Tags : "No Tags";
                var language = sourceDocument.Language.IsNotNullOrWhiteSpace() ? sourceDocument.Language : "English";
                var fileName = sourceDocument.Name.Length > 100 ? sourceDocument.Name[..100] : sourceDocument.Name;
                var fileData = AiServerTasksHelpers.GenerateStreamFromString(sourceDocument.OCRText);

                var response = await _aiOptions.DecoratorUrl
                    .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                    .WithHeader("x-api-key", _aiOptions.DocumentKey)
                    .PostMultipartAsync((body) => body
                            .AddFile("file", fileData, $"{fileName.Truncate(94)}.txt")
                            .AddString("tags", tagsData)
                            .AddString("language", language),
                        cancellationToken: cancellationToken)
                    .ReceiveJson<AiServerResponseDto>();
                return new AiResponse
                {
                    IsSuccess = true,
                    Status = SourceDocumentStatus.Decorating().Value,
                    Response = response.TaskId,
                    ActionDate = Clock.GetLocalNow().Date
                };
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call.Response.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting OCR task progress");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        public async Task<AiResponse> StartOcrTask(string Url, CancellationToken cancellationToken)
        {
            try
            {
                if (_aiOptions.DocumentKey == null)
                {
                    throw new Exception("StartOcrTask: _aiOptions.DocumentKey is null");
                }

                if (Url == null)
                {
                    throw new Exception("StartOcrTask: Url is null");
                }

                var request = new AiServerRequestDto(Url).Get();
                var response = await _aiOptions.ProcessDocumentUrl
                    .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                    .WithHeader("x-api-key", _aiOptions.DocumentKey)
                    .PostJsonAsync(request, cancellationToken: cancellationToken)
                    .ReceiveJson<AiServerResponseDto>();

                return new AiResponse
                {
                    IsSuccess = true,
                    Status = SourceDocumentStatus.OCR().Value,
                    Response = response.TaskId,
                    ActionDate = Clock.GetLocalNow().Date
                };
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call.Response.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call.Response.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting OCR task");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        public async Task<AiResponse> StartTrainingTask(SourceDocumentDto sourceDocument,
            CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Starting Training task for document {DocumentId}", sourceDocument.Id);
                if (_aiOptions.TrainIndexName == null)
                {
                    throw new Exception("StartTrainingTask: _aiOptions.TrainIndexName is null");
                }

                if (_aiOptions.TrainNamespace == null)
                {
                    throw new Exception("StartTrainingTask: _aiOptions.TrainNamespace is null");
                }

                if (!sourceDocument.Id.ToString().IsNotNullOrWhiteSpace())
                {
                    throw new Exception("StartTrainingTask: sourceDocument.Id.ToString() is null");
                }

                if (_aiOptions.UseVectorLinkImporter)
                {
                    if (vectorLinkImporter == null)
                    {
                        logger.LogError("VectorLinkImporter is not registered with DI");
                        return new()
                        {
                            IsSuccess = false,
                            Status = SourceDocumentStatus.Error().Value,
                            ErrorMessage = "VectorLinkImporter is not registered with DI",
                            ActionDate = Clock.GetLocalNow().UtcDateTime
                        };
                    }

                    try
                    {
                        List<AttachmentInfo> attachments = null;

                        var archive = sp.GetRequiredNamedService<IArchive>("pinecone.default");
                        if (sourceDocument.Citations != null)
                        {
                            attachments = new();
                            foreach (var citation in sourceDocument.Citations)
                                attachments.Add(new()
                                {
                                    CitationId = citation.Id,
                                    FileName = citation.Name,
                                    FileUrl = citation.Url,
                                    Description = citation.Description,
                                    DoGetContentStreamAsync = async () =>
                                        await (await (((PineconeChunkArchive)archive).Snapshot.Options.AttachmentsBaseUrl!?.RemoveTrailing("/") + citation.Url)
                                            .GetStreamAsync()).CopyToMemoryStreamAsync()
                                });
                        }

                        async Task<string> GetArchiveNameAsync()
                        {
                            var nodeId = sourceDocument.NodeId;
                            if (!nodeId.HasValue)
                                return null;

                            var dbContext = sp.GetService<AelaDbContext>();

                            var node = await dbContext.Nodes.FirstOrDefaultAsync(id => id.Id == nodeId.Value);

                            if (node == null)
                                return null;

                            var project = await dbContext.Projects.FirstOrDefaultAsync(id => id.Id == node.ProjectId);
                            if (project == null)
                                return null;

                            if (project.Namespace == null)
                                return null;

                            return "pinecone-namespace." + project.Namespace;
                        }

                        var archiveName = await GetArchiveNameAsync();

                        await vectorLinkImporter.ImportAsync(
                            new()
                            {
                                Description = sourceDocument.Description,
                                DocumentId = sourceDocument.Id,
                                Attachments = attachments,
                                Tags = sourceDocument.Tags?.Split(',').ToHashSet() ?? [],
                                SourceDocumentDisplayName = sourceDocument.DisplayName ?? "Unnamed Document",
                                DocumentContent = await (await sourceDocument.Url.GetStreamAsync()).CopyToMemoryStreamAsync(),
                                ExtractImages = null,
                                TrainCitations = null,
                                Archive = archiveName
                            }
                        );

                        logger.LogDebug("Document {documentId} has been trained successfully", sourceDocument.Id);

                        return new()
                        {
                            IsSuccess = true,
                            Status = SourceDocumentStatus.TrainingDone().Value,
                            Response = "done_taskId",
                            ActionDate = Clock.GetLocalNow().UtcDateTime
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "During document importing");
                        return new()
                        {
                            IsSuccess = false,
                            Status = SourceDocumentStatus.Error().Value,
                            ErrorMessage = ex.GetType().Name + ": " + ex.Message,
                            ActionDate = Clock.GetLocalNow().UtcDateTime
                        };
                    }
                }
                else
                {
                    var fileData = AiServerTasksHelpers.GenerateStreamFromString(sourceDocument.DecoratorText);

                    var response = await _aiOptions.TrainUrl
                        .WithTimeout(TimeSpan.FromSeconds(_aiOptions.HttpTimeoutInSeconds))
                        .WithHeader("x-api-key", _aiOptions.TrainApiKey)
                        .BeforeCall(BeforeCallHandler)
                        .PostMultipartAsync((content) => content
                                .AddFile("file", fileData, $"{sourceDocument.Name.Truncate(94)}.txt")
                                .AddString("description", sourceDocument.Description?.Truncate(254) ?? "Unkwown")
                                .AddString("document_id", sourceDocument.Id.ToString())
                                .AddString("index_name", _aiOptions.TrainIndexName)
                                .AddString("namespace", _aiOptions.TrainNamespace)
                                .AddJson("citation_info", sourceDocument.Citations),
                            cancellationToken: cancellationToken)
                        .ReceiveJson<AiServerResponseDto>();

                    logger.LogDebug("Document {DocumentId} has been trained successfully {TrainingId}",
                        sourceDocument.Id,
                        response.TaskId);

                    return new()
                    {
                        IsSuccess = true,
                        Status = SourceDocumentStatus.Done().Value,
                        Response = response.TaskId,
                        ActionDate = Clock.GetLocalNow().UtcDateTime
                    };
                }
            }
            catch (FlurlHttpException ex)
            {
                logger.LogError("{Message}", ex.Message);
                logger.LogError("{Message}", ex.Call?.Response?.StatusCode.ToString());
                logger.LogError("{Message}",
                    ex.Call?.Response?.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result);
                var error =
                    $"{ex.Message} {ex.Call?.Response?.ResponseMessage.Content.ReadAsStringAsync(cancellationToken).Result}";
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = error, Status = SourceDocumentStatus.Error().Value };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Starting Training task");
                return new AiResponse
                    { IsSuccess = false, ErrorMessage = ex.Message, Status = SourceDocumentStatus.Error().Value };
            }
        }

        private async Task BeforeCallHandler(FlurlCall call)
        {
            logger.LogTrace(await call.HttpRequestMessage.Content.ReadAsStringAsync());
        }

        public Task<AiResponse> StartWebScrapingTask(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AiResponse> TrainingTaskProgress(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AiResponse> WebScrapingTaskProgress(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public interface IAiServerTasks
    {
        Task<AiResponse> StartOcrTask(string Url, CancellationToken cancellationToken);
        Task<AiResponse> OcrTaskProgress(string OCRTaskID, CancellationToken cancellationToken);

        Task<AiResponse> StartWebScrapingTask(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);

        Task<AiResponse> WebScrapingTaskProgress(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);

        Task<AiResponse> StartDecoratingTask(SourceDocumentDto sourceDocument, CancellationToken cancellationToken);
        Task<AiResponse> DecoratingTaskProgress(string DecoratorTaskID, CancellationToken cancellationToken);
        Task<AiResponse> StartTrainingTask(SourceDocumentDto sourceDocument, CancellationToken cancellationToken);

        Task<AiResponse> TrainingTaskProgress(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);

        Task<AiResponse> DeleteTrainedFile(Guid Id, string pineconeNamespace, bool forceOverride,
            CancellationToken cancellationToken);

        Task<AiResponse> DeleteNameSpace(string pineconeNamespace, CancellationToken cancellationToken);
        Task<AiResponse> DeleteDecoratedData(Guid Id, CancellationToken cancellationToken);

        Task<AiResponse> SetDecoratingOptions(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);

        Task<AiResponse> SetPineconeOptions(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);

        Task<AiResponse> SetChatOptions(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);

        Task<AiResponse> GetLogsFromAI(string sourceBucket, string destinationBucket, string fileName,
            CancellationToken cancellationToken);
    }
}