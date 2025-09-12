namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetRootNode
{
    public sealed record Query(Guid ProjectId) : IRequest<List<NodeDto>>;

    public sealed class Handler : IRequestHandler<Query, List<NodeDto>>    
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _heimGuard = heimGuard;
        }        
        public async Task<List<NodeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var rootNode = await _nodeRepository.GetRootByProjectId(request.ProjectId, cancellationToken: cancellationToken);
            var result = GetNodeAndChildNodes(rootNode);

            return result.Select(node => node.ToNodeDto()).ToList();
            
        }
        private List<Node> GetNodeAndChildNodes(Node parentNode)
        {
            var result = new List<Node> { parentNode };

            foreach (var childNode in parentNode.Nodes)
            {
                result.AddRange(GetNodeAndChildNodes(childNode));
            }

            return result;
        }
    }
}