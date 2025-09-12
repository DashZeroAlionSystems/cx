namespace CX.Container.Server.Domain.ProcessSourceDocuments.Features;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocumentStatus;
using CX.Container.Server.Domain.SourceDocuments.Mappings;
using MediatR;
using Microsoft.Extensions.Options;
using CX.Container.Server.Configurations;
using CX.Container.Server.Exceptions;
using HeimGuard;
using CX.Container.Server.Wrappers;

public class DestroySingleDocument
{
    public sealed record Command(SourceDocumentDto SourceDocument) : IRequest<SourceDocumentDto>;

    public sealed class Handler(IFileProcessing fileProcessing, IOptions<AwsSystemOptions> awsOptions, ILogger<ProcessSourceDocument> logger, IHeimGuardClient heimGuard,
    ISourceDocumentRepository repo) : IRequestHandler<Command, SourceDocumentDto>
    {
        private readonly ILogger<ProcessSourceDocument> _logger = logger;
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly IFileProcessing _fileProcessing = fileProcessing;
        private readonly ISourceDocumentRepository _repo = repo;
        public string User { get; set; }
        public async Task<SourceDocumentDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);
            _logger.LogInformation("Processing source documents {Id} {Name}",
                    request.SourceDocument.Id, request.SourceDocument.Name);
            if (request.SourceDocument == null || request.SourceDocument == default)
            {
                _logger.LogInformation("No source documents to process");
                return request.SourceDocument;
            }
            var bucket = (request.SourceDocument.Status == SourceDocumentStatus.PublicBucket())
                ? _awsOptions.PublicBucket : _awsOptions.PrivateBucket;
            _logger.LogInformation("Destroy Document {Id} {Name}",
                    request.SourceDocument.Id, request.SourceDocument.Name);
            await _fileProcessing.DeleteFileAsync(bucket, request.SourceDocument.Name, cancellationToken);
            var responceDocument = await _repo.GetById(request.SourceDocument.Id, cancellationToken: cancellationToken);
            return responceDocument.ToSourceDocumentDto();
        }

    }
}
