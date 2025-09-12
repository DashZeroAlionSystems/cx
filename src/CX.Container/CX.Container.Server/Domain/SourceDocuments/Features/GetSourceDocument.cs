namespace CX.Container.Server.Domain.SourceDocuments.Features;

using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetSourceDocument
{
    public sealed record Query(Guid SourceDocumentId) : IRequest<SourceDocumentDto>;

    public sealed class Handler : IRequestHandler<Query, SourceDocumentDto>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceDocumentRepository sourceDocumentRepository, IHeimGuardClient heimGuard)
        {
            _sourceDocumentRepository = sourceDocumentRepository;
            _heimGuard = heimGuard;
        }

        public async Task<SourceDocumentDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);

            var result = await _sourceDocumentRepository.GetById(request.SourceDocumentId, cancellationToken: cancellationToken);
            return result.ToSourceDocumentDto();
        }
    }
}