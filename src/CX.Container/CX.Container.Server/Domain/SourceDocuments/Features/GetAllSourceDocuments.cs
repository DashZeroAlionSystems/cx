namespace CX.Container.Server.Domain.SourceDocuments.Features;

using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllSourceDocuments
{
    public sealed record Query() : IRequest<List<SourceDocumentDto>>;

    public sealed class Handler : IRequestHandler<Query, List<SourceDocumentDto>>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceDocumentRepository sourceDocumentRepository, IHeimGuardClient heimGuard)
        {
            _sourceDocumentRepository = sourceDocumentRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<SourceDocumentDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);

            return _sourceDocumentRepository.Query()
                .AsNoTracking()
                .ToSourceDocumentDtoQueryable()
                .ToList();
        }
    }
}