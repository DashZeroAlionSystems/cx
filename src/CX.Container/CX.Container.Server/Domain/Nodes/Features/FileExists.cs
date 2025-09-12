namespace CX.Container.Server.Domain.Nodes.Features;

using MediatR;
using Microsoft.Extensions.Options;
using CX.Container.Server.Configurations;
using CX.Container.Server.Exceptions;
using HeimGuard;
using CX.Container.Server.Wrappers;

public class FileExists
{
    // The Command now takes a string for the file name
    public sealed record Command(string fileName) : IRequest<bool>;

    public sealed class Handler(IFileProcessing fileProcessing,
        IOptions<AwsSystemOptions> awsOptions, IHeimGuardClient heimGuard) : IRequestHandler<Command, bool>
    {
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly IFileProcessing _fileProcessing = fileProcessing;

        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);

            // Check if the file exists in the specified bucket
            var exists = await _fileProcessing.FileExists(_awsOptions.PublicBucket, request.fileName, cancellationToken);

            return exists;
        }
    }
}