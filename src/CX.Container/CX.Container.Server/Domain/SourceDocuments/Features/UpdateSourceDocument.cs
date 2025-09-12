using CX.Container.Server.Databases;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.SourceDocuments.Features;

using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Services;
using Mappings;
using MediatR;

public static class UpdateSourceDocument
{
    public sealed record Command(Guid SourceDocumentId, SourceDocumentForUpdateDto UpdatedSourceDocumentData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(ISourceDocumentRepository sourceDocumentRepository, IUnitOfWork unitOfWork)
        {
            _sourceDocumentRepository = sourceDocumentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var sourceDocumentToUpdate = await _sourceDocumentRepository.GetById(request.SourceDocumentId, cancellationToken: cancellationToken);
            var sourceDocumentToAdd = request.UpdatedSourceDocumentData.ToSourceDocumentForUpdate();
            sourceDocumentToUpdate.Update(sourceDocumentToAdd);

            _sourceDocumentRepository.Update(sourceDocumentToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}