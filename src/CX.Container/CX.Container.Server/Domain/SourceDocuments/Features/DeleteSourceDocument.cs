namespace CX.Container.Server.Domain.SourceDocuments.Features;

using CX.Container.Server.Databases;
using CX.Container.Server.Domain.ProcessSourceDocuments.Features;
using CX.Container.Server.Domain.SourceDocuments.Mappings;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Services;
using MediatR;

public static class DeleteSourceDocument
{
    public sealed record Command(Guid SourceDocumentId, bool IsRequestFromInfomap = false) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IMediator _mediator;
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DestroySingleDocument> _logger;

        public Handler(ISourceDocumentRepository sourceDocumentRepository, IUnitOfWork unitOfWork, IMediator mediator, ILogger<DestroySingleDocument> logger)
        {
            _sourceDocumentRepository = sourceDocumentRepository;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _unitOfWork.WrapInTransactionAsync(async (db, ct) =>
            {
                await DeleteSourceDocument(db, ct);
            }, cancellationToken);

            // local method
            async Task DeleteSourceDocument(AelaDbContext db, CancellationToken ct)
            {
                var recordToDelete = await _sourceDocumentRepository.GetWithCitations(request.SourceDocumentId,
                        cancellationToken: cancellationToken) ?? throw new NotFoundException("Source document not found");

                if (request.IsRequestFromInfomap == false && recordToDelete.NodeId != null)
                    throw new InvalidOperationException("Cannot delete a document that has been created in infomap.");
                
                var DeleteSingleDocumentDto = new DeleteSingleDocumentDto()
                {
                    SourceDocument = recordToDelete.ToSourceDocumentDto(),
                    Override = false
                };
                if (recordToDelete.Status == SourceDocumentStatus.SourceDocumentStatus.TrainingDone() ||
                    recordToDelete.Status == SourceDocumentStatus.SourceDocumentStatus.DecoratingDone() ||
                    recordToDelete.Status == SourceDocumentStatus.SourceDocumentStatus.Done())
                {
                    try
                    {
                        var deleteCommand = new DeleteSingleDocument.Command(DeleteSingleDocumentDto);
                        await _mediator.Send(deleteCommand, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting document {documentId}", recordToDelete.Id);
                    }
                }
                if (recordToDelete.Name != null)
                {
                    var destroyCommand = new DestroySingleDocument.Command(recordToDelete.ToSourceDocumentDto());
                    await _mediator.Send(destroyCommand, cancellationToken);
                }
                _sourceDocumentRepository.Remove(recordToDelete);
                await _unitOfWork.CommitChanges(cancellationToken);
            }            
        }
    }
}