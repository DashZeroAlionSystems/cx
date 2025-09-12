namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Mappings;
using MediatR;
using Microsoft.Extensions.Options;
using CX.Container.Server.Configurations;
using CX.Container.Server.Exceptions;
using HeimGuard;
using CX.Container.Server.Wrappers;

public class DestroyNodeFile
{
    public sealed record Command(NodeDto Node) : IRequest<NodeDto>;

    public sealed class Handler(IFileProcessing fileProcessing, IOptions<AwsSystemOptions> awsOptions, IHeimGuardClient heimGuard,
    INodeRepository repo) : IRequestHandler<Command, NodeDto>
    {        
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly IFileProcessing _fileProcessing = fileProcessing;
        private readonly INodeRepository _repo = repo;
        public string User { get; set; }
        public async Task<NodeDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);
            
            await _fileProcessing.DeleteFileAsync(_awsOptions.PublicBucket, request.Node.FileName, cancellationToken);
            var response = await _repo.GetById(request.Node.Id, cancellationToken: cancellationToken);
            return response.ToNodeDto();
        }
    }
}
