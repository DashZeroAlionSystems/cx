namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CX.Container.Server.Domain.Nodes.Mappings;
using CX.Container.Server.Domain.Nodes.Dtos;

public static class DeleteNode
{
    public sealed record Command(Guid NodeId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IMediator _mediator;
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(INodeRepository nodeRepository, IUnitOfWork unitOfWork, IMediator mediator, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);
            
            var recordToDelete = await _nodeRepository.GetById(request.NodeId, cancellationToken: cancellationToken) ?? throw new NotFoundException("Node not found");

            recordToDelete.ClearFileMetaData();

            _nodeRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}