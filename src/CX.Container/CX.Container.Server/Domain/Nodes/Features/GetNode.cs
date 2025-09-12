namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetNode
{
    public sealed record Query(Guid NodeId) : IRequest<NodeDto>;

    public sealed class Handler : IRequestHandler<Query, NodeDto>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _heimGuard = heimGuard;
        }
        public async Task<NodeDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var result = await _nodeRepository.GetById(request.NodeId, cancellationToken: cancellationToken);
            return result.ToNodeDto();
        }
    }
}