﻿using Aela.Server.Wrappers;

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

public class DeleteSingleDocumentDto
{
    public SourceDocumentDto SourceDocument { get; set; }
    public bool Override { get; set; }
}
public class DeleteSingleDocument
{
    public sealed record Command(DeleteSingleDocumentDto DeleteSingleDocumentDto) : IRequest<SourceDocumentDto>;

    public sealed class Handler(ILogger<DeleteSingleDocument> logger, IHeimGuardClient heimGuard, IAiServerTasks aiServerTasks,
    IUnitOfWork unitOfWork, IMediator mediator, ISourceDocumentRepository repo, IFileProcessing awsFileProcessing) : IRequestHandler<Command, SourceDocumentDto>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<DeleteSingleDocument> _logger = logger;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly IAiServerTasks _aiServerTasks = aiServerTasks;
        private readonly ISourceDocumentRepository _repo = repo;
        private readonly IFileProcessing awsFileProcessing = awsFileProcessing;

        public string User { get; set; }
        public async Task<SourceDocumentDto> Handle(Command request, CancellationToken cancellationToken)
        {
            var doc = request.DeleteSingleDocumentDto.SourceDocument;
            var force = request.DeleteSingleDocumentDto.Override;
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);
            _logger.LogInformation("Processing source documents {Id} {Name}",
                    doc.Id, doc.Name);
            if (doc == null || doc == default)
            {
                _logger.LogInformation("No source documents to process");
                return doc;
            }
            if (doc.Status == SourceDocumentStatus.TrainingDone() ||
                doc.Status == SourceDocumentStatus.DecoratingDone() ||
                doc.Status == SourceDocumentStatus.Done())
            {
                _logger.LogInformation("Delete Document {Id} {Name}",
                    doc.Id, doc.Name);
                var response = await _aiServerTasks.DeleteTrainedFile(doc.Id, string.Empty, force, cancellationToken);
                var newUrl = await awsFileProcessing.GetPresignedUrlAsync(string.Empty, doc.Name, cancellationToken);
                var responseDocument = await ProcessAndSave(doc.Id, response, newUrl, cancellationToken);
                return responseDocument;
            }

            return doc;
        }
        private async Task<SourceDocumentDto> ProcessAndSave(Guid Id,
            AiResponse aiResponse, string NewUrl, CancellationToken cancellationToken)
        {
            if (aiResponse.IsSuccess == false && aiResponse.ErrorMessage.IsNotNullOrWhiteSpace())
            {
                var noUpdateDocument = await _repo.GetById(Id, cancellationToken: cancellationToken);
                return noUpdateDocument.ToSourceDocumentDto();
            }
            var command = new UpdateSourceDocument.Command(Id, new SourceDocumentForUpdateDto()
            {
                Status = aiResponse.Status,
                OCRTaskID = string.Empty,
                OCRText = string.Empty,
                Url = NewUrl,
                DecoratorTaskID = string.Empty,
                DecoratorText = string.Empty,
                TrainingTaskID = string.Empty,
                ErrorText = string.Empty
            });
            await _mediator.Send(command, cancellationToken);
            var responceDocument = await _repo.GetById(Id, cancellationToken: cancellationToken);
            return responceDocument.ToSourceDocumentDto();
        }


    }
}