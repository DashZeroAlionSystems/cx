using CX.Container.Server.Configurations;
using CX.Container.Server.Domain.ProcessSourceDocuments.Features;
using CX.Container.Server.Domain.SourceDocuments;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Features;
using CX.Container.Server.Domain.SourceDocuments.Mappings;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Domain.SourceDocumentStatus;

using CX.Container.Server.Services;
using CX.Engine.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class ProcessSourceDocument(ILogger<ProcessSourceDocument> logger,
    IUnitOfWork unitOfWork, IMediator mediator, IOptions<AiOptions> aiOptions, ISourceDocumentRepository repo)
{
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public async Task Handle(CancellationToken cancellationToken)
    {
        if (!_aiOptions.AutoProcess)
            return;

        using var _ = await _semaphoreSlim.UseAsync();

        while (true)
        {
            var list = await repo.Query().Where(sourceDoc =>
                sourceDoc.Status != SourceDocumentStatus.TrainingDone() &&
                sourceDoc.Status != SourceDocumentStatus.Done() &&
                sourceDoc.Status != SourceDocumentStatus.PublicBucket() &&
                sourceDoc.Status != SourceDocumentStatus.Error()).ToListAsync();
            if (list.Count == 0)
            {
                await unitOfWork.CommitChanges(cancellationToken);
                return;
            }

            foreach (var sourceDocument in list)
                await ProcessDocument(sourceDocument);

            if (list.Count == 0)
                break;
        }

        await unitOfWork.CommitChanges(cancellationToken);
    }

    private async Task ProcessDocument(SourceDocument sourceDocument)
    {
        try
        {
            var sourceDocumentDto = sourceDocument.ToSourceDocumentDto();
            var command = new ProcessSingleDocument.Command(sourceDocumentDto);
            var commandResponse = await mediator.Send(command);
            logger.LogInformation(
                "Processed Source Document with id {SourceDocumentId} from status {status} pointing to {Url}, {commandResponse}",
                sourceDocumentDto.Id,
                sourceDocumentDto.Status,
                sourceDocumentDto.Url,
                commandResponse.Name);
        }
        catch (Exception ex)
        {
            logger.LogError("Document {Id}, {Name} failed to process {Message}",
                sourceDocument.Id,
                sourceDocument.Name,
                ex.Message);
            await SetErrorStateAsync(sourceDocument, ex);
        }
    }

    private async Task SetErrorStateAsync(SourceDocument sourceDocument, Exception ex)
    {
        try
        {
            var updateDto = new SourceDocumentForUpdateDto
            {
                Status = SourceDocumentStatus.Error(),
                ErrorText = ex.Message
            };
            logger.LogInformation("Setting {Id}, {Name} to error state",
                sourceDocument.Id,
                sourceDocument.Name);
            var updateCommand = new UpdateSourceDocument.Command(sourceDocument.Id, updateDto);
            await mediator.Send(updateCommand);
        }
        catch (Exception)
        {
            logger.LogError("Failed to set {Id}, {Name} to error state",
                sourceDocument.Id,
                sourceDocument.Name);
        }
    }
}