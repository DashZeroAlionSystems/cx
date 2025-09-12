using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aela.Server.Wrappers;
using CX.Container.Server.Configurations;
using CX.Container.Server.Domain.DocumentSourceTypes;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Features;
using CX.Container.Server.Domain.SourceDocuments.Mappings;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Extensions.Application;
using CX.Container.Server.Services;
using CX.Container.Server.Wrappers;
using CX.Engine.Common;
using CX.Engine.DocExtractors.Text;
using Flurl.Http;
using MediatR;
using Microsoft.Extensions.Options;

namespace CX.Container.Server.Domain.ProcessSourceDocuments.Features;
public static class TextSanitizer
{
    // Remove null characters from the string
    private static string RemoveNullCharacters(string input)
    {
        return input.Replace("\0", string.Empty);
    }

    // Validate and ensure the string is UTF-8 encoded
    private static string ValidateUtf8Encoding(string input)
    {
        // Encode the string to UTF-8 bytes
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(input);

        // Decode the bytes back to a string
        string validatedString = Encoding.UTF8.GetString(utf8Bytes);

        return validatedString;
    }
    // Remove invalid UTF-8 characters using Regex
    public static string RemoveInvalidUtf8Characters(string input)
    {
        // Pattern to match valid UTF-8 characters
        string pattern = @"[^\u0000-\u007F\u0080-\u07FF\u0800-\uFFFF\u10000-\u10FFFF]";

        return Regex.Replace(input, pattern, string.Empty);
    }

    public static string SanitizeText(this string input)
    {
        // Remove null characters
        string sanitizedText = RemoveNullCharacters(input);

        // Remove invalid UTF-8 characters
        sanitizedText = RemoveInvalidUtf8Characters(sanitizedText);

        // Validate UTF-8 encoding
        sanitizedText = ValidateUtf8Encoding(sanitizedText);

        return sanitizedText;
    }
}

public class ProcessSingleDocument
{
    private static readonly SemaphoreSlim _maxConcurrencyLock = new(1, 1);

    public sealed record Command(SourceDocumentDto SourceDocument) : IRequest<SourceDocumentDto>;

    public sealed class Handler(
        IFileProcessing fileProcessing,
        IOptions<AwsSystemOptions> awsOptions,
        ILogger<ProcessSourceDocument> logger,
        IAiServerTasks aiServerTasks,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ISourceDocumentRepository repo,
        IOptions<AiOptions> aiOptions,
        PDFPlumber extractor,
        TimeProvider clock) : IRequestHandler<Command, SourceDocumentDto>
    {
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private IFileProcessing FileProcessing { get; set; } = fileProcessing;
        public string User { get; set; }

        public async Task<SourceDocumentDto> Handle(Command request, CancellationToken cancellationToken)
        {
            async Task<AiResponse> ExtractTextFromDocument()
            {
                logger.LogInformation("Extracting text from document {Id} {Name}",
                    request.SourceDocument.Id,
                    request.SourceDocument.Name);

                AiResponse aiResponse;

                try
                {
                    var stream = await (await request.SourceDocument.Url.GetStreamAsync()).CopyToMemoryStreamAsync();
                    var extractedText = await extractor.ExtractToTextAsync(stream, new());
                    aiResponse = new()
                    {
                        IsSuccess = true,
                        Status = SourceDocumentStatus.SourceDocumentStatus.DecoratingDone().Value,
                        Response = extractedText,
                        ActionDate = clock.GetLocalNow().Date
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "During text extraction from document");
                    aiResponse = new()
                    {
                        IsSuccess = false,
                        Status = SourceDocumentStatus.SourceDocumentStatus.Error().Value,
                        ErrorMessage = ex.GetType().Name + ": " + ex.Message,
                        ActionDate = clock.GetLocalNow().Date
                    };
                }

                return aiResponse;
            }

            if (!_maxConcurrencyLock.Wait(0))
            {
                logger.LogInformation("Queueing document {Id} {Name} with status {Status}",
                    request.SourceDocument.Id,
                    request.SourceDocument.Name,
                    request.SourceDocument.Status);
                await _maxConcurrencyLock.WaitAsync();
            }

            try
            {
                logger.LogInformation("Processing document {Id} {Name} with status {Status}",
                    request.SourceDocument.Id,
                    request.SourceDocument.Name,
                    request.SourceDocument.Status);
                if (request.SourceDocument == null || request.SourceDocument == default)
                {
                    logger.LogError("No source documents to process");
                    var response = new AiResponse
                    {
                        IsSuccess = false,
                        Status = SourceDocumentStatus.SourceDocumentStatus.Error().Value,
                        ErrorMessage = "No source documents to process",
                        ActionDate = clock.GetLocalNow().Date
                    };
                    var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                    return responseDocument;
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.PublicBucket() &&
                         request.SourceDocument.DocumentSourceType == DocumentSourceType.Blob())
                {
                    logger.LogInformation("Moving Document From Public to Private {Id} {Name}",
                        request.SourceDocument.Id,
                        request.SourceDocument.Name);

                    await AwsMoveFile(request.SourceDocument, cancellationToken);

                    var responseDocument = await repo.GetById(request.SourceDocument.Id, cancellationToken: cancellationToken);

                    return responseDocument.ToSourceDocumentDto();
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.PublicBucket() &&
                         request.SourceDocument.DocumentSourceType == DocumentSourceType.Site())
                {
                    logger.LogInformation("Moving Document From Public to Private {Id} {Name}",
                        request.SourceDocument.Id,
                        request.SourceDocument.Name);
                    //await AwsMoveFile(request, cancellationToken);
                    //TODO: Web Scraping
                    var responceDocument = await repo.GetById(request.SourceDocument.Id, cancellationToken: cancellationToken);
                    return responceDocument.ToSourceDocumentDto();
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.Scraping())
                {
                    logger.LogInformation("Moving Document From Public to Private {Id} {Name}",
                        request.SourceDocument.Id,
                        request.SourceDocument.Name);
                    //await AwsMoveFile(request, cancellationToken);
                    //TODO: Web Scraping
                    var responceDocument = await repo.GetById(request.SourceDocument.Id, cancellationToken: cancellationToken);
                    return responceDocument.ToSourceDocumentDto();
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.PrivateBucket() ||
                         request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.QueuedForRetrain())
                {
                    //Patch for long running training Henzard 2024-07-03
                    var doc = await UpdatePresignedUrlAsync(request.SourceDocument, cancellationToken);
                    request.SourceDocument.Url = doc.Url;
                    if (aiOptions.Value.UseVectorLinkDocumentExtractors)
                    {
                        var response = await ExtractTextFromDocument();
                        var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                        return responseDocument;
                    }
                    else
                    {
                        logger.LogInformation("Sending PDF for OCR {Id} {Name}",
                            request.SourceDocument.Id,
                            request.SourceDocument.Name);
                        var response = await aiServerTasks.StartOcrTask(request.SourceDocument.Url, cancellationToken);
                        var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                        return responseDocument;
                    }
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.OCR())
                {
                    if (aiOptions.Value.UseVectorLinkDocumentExtractors)
                    {
                        var response = new AiResponse
                        {
                            IsSuccess = false,
                            Status = SourceDocumentStatus.SourceDocumentStatus.Error(),
                            ErrorMessage = "OCR is not a step in the VectorLink importer pipeline",
                            ActionDate = clock.GetLocalNow().Date
                        };
                        var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                        return responseDocument;
                    }
                    else
                    {
                        logger.LogInformation("OCR Completed {Id} {Name}",
                            request.SourceDocument.Id,
                            request.SourceDocument.Name);
                        var response = await aiServerTasks.OcrTaskProgress(request.SourceDocument.OCRTaskID, cancellationToken);
                        var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                        return responseDocument;
                    }
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.OCRDone())
                {
                    logger.LogInformation("Decorating OCR Text {Id} {Name}",
                        request.SourceDocument.Id,
                        request.SourceDocument.Name);
                    var response = await aiServerTasks.StartDecoratingTask(request.SourceDocument, cancellationToken);
                    var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                    return responseDocument;
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.Decorating())
                {
                    logger.LogInformation("Decorating Completed {Id} {Name}",
                        request.SourceDocument.Id,
                        request.SourceDocument.Name);
                    var response = await aiServerTasks.DecoratingTaskProgress(request.SourceDocument.DecoratorTaskID, cancellationToken);
                    var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                    return responseDocument;
                }
                else if (request.SourceDocument.Status == SourceDocumentStatus.SourceDocumentStatus.DecoratingDone())
                {
                    logger.LogInformation("Training Data Id: {Id}, Name:{Name}",
                        request.SourceDocument.Id,
                        request.SourceDocument.Name);

                    var sourceDocument = await repo.GetWithCitations(request.SourceDocument.Id);
                    var response = await aiServerTasks.StartTrainingTask(sourceDocument.ToSourceDocumentDto(), cancellationToken);
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    logger.LogDebug("Response Is {response}", JsonSerializer.Serialize(response, options));
                    var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                    return responseDocument;
                }
                else
                {
                    logger.LogError($"Unsupported document state found: {request.SourceDocument.Status}");
                    var response = new AiResponse
                    {
                        IsSuccess = false,
                        Status = SourceDocumentStatus.SourceDocumentStatus.Error().Value,
                        ErrorMessage = $"Unsupported document state found: {request.SourceDocument.Status}",
                        ActionDate = clock.GetLocalNow().Date
                    };
                    var responseDocument = await ProcessAndSave(request.SourceDocument.Id, response, cancellationToken);
                    return responseDocument;
                }
            }
            finally
            {
                _maxConcurrencyLock.Release();
            }
        }
        private async Task<SourceDocumentDto> ProcessAndSave(Guid Id, AiResponse aiResponse, CancellationToken cancellationToken)
        {
            logger.LogDebug("Processing AI Response Id:{Id}, Status:{Status}, Response:{Response}, ErrorMessage:{ErrorMessage}",
                Id,
                aiResponse.Status,
                aiResponse.Response,
                aiResponse.ErrorMessage);
            if (aiResponse.IsSuccess == false && !aiResponse.ErrorMessage.IsNotNullOrWhiteSpace())
            {
                var noUpdateDocument = await repo.GetById(Id, cancellationToken: cancellationToken);
                return noUpdateDocument.ToSourceDocumentDto();
            }
            //Sanitise Text

            var statusText = aiResponse.Status.Value;
            var command = new UpdateSourceDocument.Command(Id,
                new SourceDocumentForUpdateDto
                {
                    Status = aiResponse.Status
                });
            switch (statusText)
            {
                case "OCR":
                    command.UpdatedSourceDocumentData.OCRTaskID = aiResponse.Response.SanitizeText();
                    break;
                case "OCRDone":
                    command.UpdatedSourceDocumentData.OCRText = aiResponse.Response.SanitizeText();
                    break;
                case "Decorating":
                    command.UpdatedSourceDocumentData.DecoratorTaskID = aiResponse.Response.SanitizeText();
                    break;
                case "DecoratingDone":
                    command.UpdatedSourceDocumentData.DecoratorText = aiResponse.Response.SanitizeText();
                    break;
                case "Training":
                    logger.LogWarning("Training In Progress");
                    break;
                case "TrainingDone":
                    logger.LogWarning("Training Done");
                    break;
                case "Scraping":
                    logger.LogWarning("Scraping In Progress");
                    break;
                case "ScrapingDone":
                    logger.LogWarning("Scraping Done");
                    break;
                case "QueuedForRetrain":
                    command.UpdatedSourceDocumentData.OCRTaskID = string.Empty;
                    command.UpdatedSourceDocumentData.OCRText = string.Empty;
                    command.UpdatedSourceDocumentData.DecoratorTaskID = string.Empty;
                    command.UpdatedSourceDocumentData.DecoratorText = string.Empty;
                    command.UpdatedSourceDocumentData.TrainingTaskID = string.Empty;
                    command.UpdatedSourceDocumentData.ErrorText = string.Empty;
                    break;
                case "Done":
                    command.UpdatedSourceDocumentData.TrainingTaskID = aiResponse.Response.SanitizeText();
                    command.UpdatedSourceDocumentData.DateTrained = aiResponse.ActionDate;
                    break;
                case "Error":
                    command.UpdatedSourceDocumentData.ErrorText = aiResponse.ErrorMessage.SanitizeText();
                    logger.LogWarning("Error");
                    break;
            }

            try
            {
                await mediator.Send(command, cancellationToken);
                var responceDocument = await repo.GetById(Id, cancellationToken: cancellationToken);
                return responceDocument.ToSourceDocumentDto();
            }
            catch (Exception ex)
            {
                var noUpdate = await repo.GetById(Id, cancellationToken: cancellationToken);
                logger.LogError(ex, "Error ProcessAndSave task");
                return noUpdate.ToSourceDocumentDto();
            }
        }

        private async Task AwsMoveFile(SourceDocumentDto sourceDocument, CancellationToken cancellationToken)
        {
            await FileProcessing.CopyFileAsync(_awsOptions.PublicBucket, _awsOptions.PrivateBucket, sourceDocument.Name, cancellationToken);
            await FileProcessing.DeleteFileAsync(_awsOptions.PublicBucket, sourceDocument.Name, cancellationToken);
            await UpdatePresignedUrlAsync(sourceDocument, cancellationToken);
        }
        private async Task<SourceDocumentDto> UpdatePresignedUrlAsync(SourceDocumentDto sourceDocument, CancellationToken cancellationToken)
        {
            var preSharedUrl = await FileProcessing.GetPresignedUrlAsync(_awsOptions.PrivateBucket, sourceDocument.Name, cancellationToken);
            var command = new UpdateSourceDocument.Command(sourceDocument.Id,
                new SourceDocumentForUpdateDto
                {
                    Status = SourceDocumentStatus.SourceDocumentStatus.PrivateBucket().Value,
                    Url = preSharedUrl
                });

            await mediator.Send(command, cancellationToken);
            await unitOfWork.CommitChanges(cancellationToken);
            sourceDocument.Url = preSharedUrl;
            return sourceDocument;
        }
    }
}