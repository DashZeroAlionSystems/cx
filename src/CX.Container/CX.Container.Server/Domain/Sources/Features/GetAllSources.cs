namespace CX.Container.Server.Domain.Sources.Features;

using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Domain.Sources.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllSources
{
    public sealed record Query() : IRequest<List<SourceDto>>;

    public sealed class Handler : IRequestHandler<Query, List<SourceDto>>
    {
        private readonly ISourceRepository _sourceRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceRepository sourceRepository, IHeimGuardClient heimGuard)
        {
            _sourceRepository = sourceRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<SourceDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSources);

            return _sourceRepository.Query()
                .AsNoTracking()
                .ToSourceDtoQueryable()
                .ToList();
        }
    }
}