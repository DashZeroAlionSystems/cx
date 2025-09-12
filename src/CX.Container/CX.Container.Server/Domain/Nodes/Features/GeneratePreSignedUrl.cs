namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;
using CX.Container.Server.Configurations;
using Microsoft.Extensions.Options;
using CX.Container.Server.Wrappers;

public static class GeneratePreSignedUrl
{
    public sealed record Command(Guid NodeId, NodeForUpdateS3Dto UpdatedNodeData) : IRequest;

    public sealed class Handler(INodeRepository nodeRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard, IOptions<AwsSystemOptions> awsOptions, IFileProcessing fileProcessing) : IRequestHandler<Command>
    {
        private readonly INodeRepository _nodeRepository = nodeRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly IFileProcessing _fileProcessing = fileProcessing;

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var nodeToUpdate = await _nodeRepository.GetById(request.NodeId, cancellationToken: cancellationToken);

            request.UpdatedNodeData.S3Key = await _fileProcessing.GetPresignedUrlAsync(_awsOptions.PublicBucket, request.UpdatedNodeData.FileName, cancellationToken);

            nodeToUpdate.SetS3Key(request.UpdatedNodeData.S3Key, request.UpdatedNodeData.FileName, request.UpdatedNodeData.DisplayName);            
           
            _nodeRepository.Update(nodeToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);           
        }
    }
}