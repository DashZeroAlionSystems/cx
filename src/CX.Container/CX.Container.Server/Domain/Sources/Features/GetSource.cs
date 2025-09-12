namespace CX.Container.Server.Domain.Sources.Features;

using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Domain.Sources.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetSource
{
    public sealed record Query(Guid SourceId) : IRequest<SourceDto>;

    public sealed class Handler : IRequestHandler<Query, SourceDto>
    {
        private readonly ISourceRepository _sourceRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceRepository sourceRepository, IHeimGuardClient heimGuard)
        {
            _sourceRepository = sourceRepository;
            _heimGuard = heimGuard;
        }

        public async Task<SourceDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSources);

            var result = await _sourceRepository.GetById(request.SourceId, cancellationToken: cancellationToken);
            return result.ToSourceDto();
        }
    }
}