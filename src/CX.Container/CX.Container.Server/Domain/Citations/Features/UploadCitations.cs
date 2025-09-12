namespace CX.Container.Server.Domain.ProcessSourceDocuments.Features;

using CX.Container.Server.Domain.Citations;
using CX.Container.Server.Domain.Citations.Dtos;
using CX.Container.Server.Domain.Citations.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Services;
using HeimGuard;
using MediatR;

public class UploadCitations
{
    public sealed record Command(CitationUploadDto dto) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ICitationRepository _citationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;
        private readonly ILogger<Handler> _logger;

        public Handler(
            ICitationRepository citationRepository,
            IUnitOfWork unitOfWork,
            IHeimGuardClient heimGuard,
            ILogger<Handler> logger)
        {
            _logger = logger;
            _citationRepository = citationRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);

            await _citationRepository.Add(Citation.Create(request.dto.SourceDocumentId, request.dto));
            await _unitOfWork.CommitChanges(cancellationToken);

            _logger.LogDebug("Citations for SourceDocument with id {SourceDocumentId} was added.", request.dto.SourceDocumentId);
        }
    }
}
