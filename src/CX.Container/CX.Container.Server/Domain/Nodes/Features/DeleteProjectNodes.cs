namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Microsoft.EntityFrameworkCore;
using MediatR;
using CX.Container.Server.Services;

public static class DeleteProjectNodes
{
    /// <summary>
    /// Command to delete all nodes associated with a specified project.
    /// </summary>
    /// <param name="ProjectId">The ID of the project whose nodes are to be deleted.</param>
    public sealed record Command(Guid ProjectId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(INodeRepository nodeRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var nodesToDelete = await _nodeRepository.Query()
                .Where(node => node.ProjectId == request.ProjectId)
                .ToListAsync(cancellationToken);

            if (nodesToDelete.Count > 0)
            {
                _nodeRepository.RemoveRange(nodesToDelete);
                await _unitOfWork.CommitChanges(cancellationToken);
            }
        }
    }
}
