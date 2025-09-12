namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using MediatR;
using Microsoft.Extensions.Options;
using CX.Container.Server.Configurations;
using CX.Container.Server.Exceptions;
using HeimGuard;
using CX.Container.Server.Wrappers;

public class UploadNodeDocument
{
    public sealed record Command(NodeForFileUploadDto node) : IRequest<NodeForUpdateS3Dto>;

    public sealed class Handler(IFileProcessing fileProcessing, ILogger<UploadNodeDocument> logger,
        IOptions<AwsSystemOptions> awsOptions, IHeimGuardClient heimGuard) : IRequestHandler<Command, NodeForUpdateS3Dto>
    {        
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly ILogger<UploadNodeDocument> _logger = logger;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly IFileProcessing _fileProcessing = fileProcessing;        
        public async Task<NodeForUpdateS3Dto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);
            
            var resp = await _fileProcessing.UploadFileAsync(_awsOptions.PublicBucket, request.node.File, cancellationToken);

            if(resp == null)
            {
                _logger.LogError("Error uploading file {fileName}", request.node.File.FileName);
                throw new Exception($"Error uploading file {request.node.File.FileName}");
            }
            
            var md5FileName = await _fileProcessing.MD5CheckSumFileName(request.node.File);
            var s3Key = await _fileProcessing.GetPresignedUrlAsync(_awsOptions.PublicBucket, md5FileName, cancellationToken);

            return new NodeForUpdateS3Dto { S3Key = s3Key, FileName = md5FileName, DisplayName = request.node.File.FileName };
        }
    }
}
