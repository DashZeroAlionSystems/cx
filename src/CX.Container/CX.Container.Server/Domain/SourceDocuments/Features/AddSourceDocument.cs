namespace CX.Container.Server.Domain.SourceDocuments.Features;

using CX.Container.Server.Domain.Citations.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Services;
using Dtos;
using HeimGuard;
using Mappings;
using MediatR;
using Services;
using SourceDocuments;

public static class AddSourceDocument
{
    public sealed record Command(SourceDocumentForCreationDto SourceDocumentToAdd) : IRequest<SourceDocumentDto>;

    public sealed class Handler : IRequestHandler<Command, SourceDocumentDto>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        private readonly ICitationRepository _citationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;
        private readonly ILogger<Handler> _logger;
        //private readonly IPublishEndpoint _publishEndpoint;

        public Handler(
            ISourceDocumentRepository sourceDocumentRepository,
            ICitationRepository citationRepository,
            IUnitOfWork unitOfWork,
            IHeimGuardClient heimGuard,
            ILogger<Handler> logger)
        {
            _logger = logger;
            _sourceDocumentRepository = sourceDocumentRepository;
            _citationRepository = citationRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task<SourceDocumentDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);

            var sourceDocumentToAdd = request.SourceDocumentToAdd.ToSourceDocumentForCreation();
            var sourceDocument = SourceDocument.Create(sourceDocumentToAdd);

            await _sourceDocumentRepository.Add(sourceDocument, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            _logger.LogDebug("SourceDocument with id {SourceDocumentId} was added pointing to {Url}", sourceDocument.Id, sourceDocument.Url);

            return sourceDocument.ToSourceDocumentDto();
        }
    }
}