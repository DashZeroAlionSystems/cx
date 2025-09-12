namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;

public static class UpdateNodeAndDocument
{
    public sealed record Command(Guid NodeId, NodeForUpdateS3Dto UpdatedNodeData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;
        private readonly ISourceDocumentService _sourceDocumentService;

        public Handler(INodeRepository nodeRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard, ISourceDocumentService sourceDocumentService)
        {
            _nodeRepository = nodeRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
            _sourceDocumentService = sourceDocumentService;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var nodeToUpdate = await _nodeRepository.GetById(request.NodeId, cancellationToken: cancellationToken);
                                       
            nodeToUpdate.SetS3Key(request.UpdatedNodeData.S3Key, request.UpdatedNodeData.FileName, request.UpdatedNodeData.DisplayName);

            await _sourceDocumentService.UpdateOrCreateSourceDocumentAsync(request.NodeId, request.UpdatedNodeData);

            _nodeRepository.Update(nodeToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}