using Aela.Server.Wrappers;

namespace CX.Container.Server.Domain.ProcessSourceDocuments.Features;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocumentStatus;
using CX.Container.Server.Domain.SourceDocuments.Mappings;
using CX.Container.Server.Domain.SourceDocuments.Features;
using MediatR;
using CX.Container.Server.Exceptions;
using HeimGuard;
using CX.Container.Server.Wrappers;
using CX.Container.Server.Extensions.Application;
using CX.Container.Server.Configurations;
using Microsoft.Extensions.Options;

public class ResetSingleDocumentDto
{
    public SourceDocumentDto SourceDocument { get; set; }
    public SourceDocumentStatus Status { get; set; }
}
public class ResetSingleDocument
{
    public sealed record Command(ResetSingleDocumentDto DeleteSingleDocumentDto) : IRequest<SourceDocumentDto>;

    public sealed class Handler(ILogger<ResetSingleDocument> logger, IMediator mediator, IOptions<AwsSystemOptions> awsOptions,
        ISourceDocumentRepository repo, IFileProcessing awsFileProcessing, IAiServerTasks aiServerTasks) : IRequestHandler<Command, SourceDocumentDto>
    {
        private readonly IMediator _mediator = mediator;
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly ILogger<ResetSingleDocument> _logger = logger;
        private readonly ISourceDocumentRepository _repo = repo;
        private readonly IFileProcessing awsFileProcessing = awsFileProcessing;
        private readonly IAiServerTasks _aiServerTasks = aiServerTasks;

        public string User { get; set; }
        public async Task<SourceDocumentDto> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Resetting Error State");
            _ = await _aiServerTasks.DeleteTrainedFile(request.DeleteSingleDocumentDto.SourceDocument.Id, string.Empty,
                true, cancellationToken);
            var command = new UpdateSourceDocument.Command(request.DeleteSingleDocumentDto.SourceDocument.Id,
                new SourceDocumentForUpdateDto()
                {
                    Status = request.DeleteSingleDocumentDto.Status.Value,
                    OCRTaskID = "--",
                    OCRText = "--",
                    Url = await awsFileProcessing.GetPresignedUrlAsync(_awsOptions.PrivateBucket,
                            request.DeleteSingleDocumentDto.SourceDocument.Name, cancellationToken),
                    DecoratorTaskID = "--",
                    DecoratorText = "--",
                    TrainingTaskID = "--",
                    ErrorText = "--"
                });
            await _mediator.Send(command, cancellationToken);
            var responceDocument = await _repo.GetById(request.DeleteSingleDocumentDto.SourceDocument.Id,
                cancellationToken: cancellationToken);
            return responceDocument.ToSourceDocumentDto();
        }



    }
}