using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Features;
using MediatR;

namespace CX.Container.Server.Services;

public class SourceDocumentService : ISourceDocumentService
{
    private readonly IMediator _mediator;

    public SourceDocumentService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task UpdateOrCreateSourceDocumentAsync(Guid nodeId, NodeForUpdateS3Dto nodeForUpdateS3)
    {
        var query = new GetSourceDocumentByNodeId.Query(nodeId);
        var sourceDocumentDto = await _mediator.Send(query);

        if (sourceDocumentDto != null)
        {
            var sourceDocumentForUpdateDto = new SourceDocumentForUpdateDto
            {
                Name = nodeForUpdateS3.FileName,
                DisplayName = nodeForUpdateS3.DisplayName,
                Url = nodeForUpdateS3.S3Key
            };

            var updateSourceDocumentCommand = new UpdateSourceDocument.Command(sourceDocumentDto.Id, sourceDocumentForUpdateDto);
            await _mediator.Send(updateSourceDocumentCommand);
        }
        else
        {
            var sourceDocumentForCreation = new SourceDocumentForCreationDto
            {
                NodeId = nodeId,
                DocumentSourceType = "Blob",
                Name = nodeForUpdateS3.FileName,
                DisplayName = nodeForUpdateS3.DisplayName,
                Url = nodeForUpdateS3.S3Key
            };
            var commandCreateSourceDocumentRecord = new AddSourceDocument.Command(sourceDocumentForCreation);
            await _mediator.Send(commandCreateSourceDocumentRecord);
        }
    }
}