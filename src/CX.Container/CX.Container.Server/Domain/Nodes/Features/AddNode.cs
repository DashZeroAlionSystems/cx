namespace CX.Container.Server.Domain.Nodes.Features;

using Services;
using Nodes;
using Dtos;
using CX.Container.Server.Services;
using Exceptions;
using Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddNode
{
    public sealed record Command(NodeForCreationDto NodeToAdd) : IRequest<NodeDto>;

    public sealed class Handler : IRequestHandler<Command, NodeDto>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;
        private readonly ILogger<Handler> _logger;        

        public Handler(
            INodeRepository sourceDocumentRepository,
            IUnitOfWork unitOfWork,
            IHeimGuardClient heimGuard,
            ILogger<Handler> logger)
        {
            _logger = logger;
            _nodeRepository = sourceDocumentRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }
    
        public async Task<NodeDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var nodeToAdd = request.NodeToAdd.ToNodeForCreation();
            var node = Node.Create(nodeToAdd);

            await _nodeRepository.Add(node, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);
            
            if(node.IsAsset)
            {
                _logger.LogDebug("File Node with id {NodeId} was added pointing to {Url}", node.Id, node.Url);
            }
            else
            {
                _logger.LogDebug("Folder Node with id {NodeId} was added", node.Id);
            }

            return node.ToNodeDto();
        }
    }
}