namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;

public static class UpdateNodeParent
{
    public sealed record Command(Guid NodeId, NodeForUpdateParentDto UpdatedNodeData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var nodeToUpdate = await _nodeRepository.GetById(request.NodeId, cancellationToken: cancellationToken);

            // Check if ParentId is provided and update the parent
            if (request.UpdatedNodeData.ParentId.HasValue)
            {
                var parentNode = await _nodeRepository.GetById(request.UpdatedNodeData.ParentId.Value, cancellationToken: cancellationToken);
                nodeToUpdate.SetParentNode(parentNode);
            }
            else
            {
                // If ParentNodeId is not provided, set the parent to null (root folder)
                nodeToUpdate.SetParentNode(null);
            }

            _nodeRepository.Update(nodeToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}