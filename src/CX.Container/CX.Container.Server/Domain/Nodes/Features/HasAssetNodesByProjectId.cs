namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;

public static class HasAssetNodesByProjectId
{
    public sealed record Query(Guid ProjectId) : IRequest<bool>;

    public sealed class Handler : IRequestHandler<Query, bool>    
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _heimGuard = heimGuard;
        }
        public async Task<bool> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var result = await _nodeRepository.HasAssetNodesByProjectId(request.ProjectId, cancellationToken: cancellationToken);

            return result;            
        }
    }
}