namespace CX.Container.Server.Domain.ProcessSourceDocuments.Features;

using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Features;
using MediatR;
using Microsoft.Extensions.Options;
using CX.Container.Server.Configurations;

using CX.Container.Server.Exceptions;
using HeimGuard;

using CX.Container.Server.Wrappers;
using CX.Container.Server.Extensions.Application;
using CX.Container.Server.Domain.SourceDocuments.Services;
using System.Text;
using CX.Container.Server.Domain.Citations.Features;

public class UploadAndCreateDocument
{
    public sealed record Command(SourceDocumentForFileUploadDto sourceDocument) : IRequest<SourceDocumentForFileUploadDto>;

    public sealed class Handler(IFileProcessing fileProcessing, ILogger<UploadAndCreateDocument> logger, IOptions<AwsSystemOptions> awsOptions, IHeimGuardClient heimGuard,
     IMediator mediator) : IRequestHandler<Command, SourceDocumentForFileUploadDto>
    {
        private readonly IMediator _mediator = mediator;
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly ILogger<UploadAndCreateDocument> _logger = logger;
        private readonly IHeimGuardClient _heimGuard = heimGuard;
        private readonly IFileProcessing _fileProcessing = fileProcessing;
        public string User { get; set; }

        public async Task<SourceDocumentForFileUploadDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);
            var md5FileName = request.sourceDocument.File.FileName;
            if (request.sourceDocument.File.ContentType == "application/pdf")
            {
                if (await _fileProcessing.FileExists(_awsOptions.PrivateBucket, request.sourceDocument.File.FileName, cancellationToken))
                {
                    _logger.LogError("File {fileName} already exists", request.sourceDocument.File.FileName);
                    throw new Exception($"File {request.sourceDocument.File.FileName} already exists");
                }

                var resp = await _fileProcessing.UploadFileAsync(_awsOptions.PublicBucket, request.sourceDocument.File, cancellationToken);
                if (resp == null)
                {
                    _logger.LogError("Error uploading file {fileName}", request.sourceDocument.File.FileName);
                    throw new Exception($"Error uploading file {request.sourceDocument.File.FileName}");
                }

                md5FileName = await _fileProcessing.MD5CheckSumFileName(request.sourceDocument.File);
            }

            var sourceDocumentForCreation = new SourceDocumentForCreationDto()
            {
                Name = md5FileName,
                DisplayName = request.sourceDocument.DisplayName,
                Description = request.sourceDocument.Description,
                Language = "English",
                DocumentSourceType = "Blob",
                Url = "https://example.com/non_sense_url/" + request.sourceDocument.File.FileName,
                SourceId = request.sourceDocument.SourceId,
                Tags = request.sourceDocument.Description,
                NodeId = request.sourceDocument.NodeId
            };

            var command = new AddSourceDocument.Command(sourceDocumentForCreation);
            var commandResponse = await _mediator.Send(command, cancellationToken);

            if (request.sourceDocument.File.ContentType == "application/pdf")
            {
                var commandProcess = new ProcessSingleDocument.Command(commandResponse);
                var commandResponseProcess = await _mediator.Send(commandProcess, cancellationToken);

                _logger.LogInformation("Processed Source Document with id {SourceDocumentId} pointing to {Url}, {commandResponse}",
                                   commandResponse.Id, commandResponse.Url, commandResponseProcess.Name);
               
                return new SourceDocumentForFileUploadDto()
                {
                    Id = commandResponse.Id,
                    DisplayName = commandResponseProcess.DisplayName,
                    Description = commandResponseProcess.Description,
                    SourceId = commandResponseProcess.SourceId
                };
            }

            var fileContent = await ReadAsStringAsync(request.sourceDocument.File);
            var sourceDocumentForUpdate = new SourceDocumentForUpdateDto() { 
                Status = SourceDocumentStatus.SourceDocumentStatus.OCRDone(),
                OCRText = fileContent
            };
            var updateCommand = new UpdateSourceDocument.Command(commandResponse.Id, sourceDocumentForUpdate);
            await _mediator.Send(updateCommand, cancellationToken);
            return new SourceDocumentForFileUploadDto()
            {
                Id = commandResponse.Id,
                DisplayName = commandResponse.DisplayName,
                Description = commandResponse.Description,
                SourceId = commandResponse.SourceId
            };
        }
        public async Task<string> ReadAsStringAsync(IFormFile file)
        {
            var result = new StringBuilder();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(await reader.ReadLineAsync());
            }
            return result.ToString();
        }
    }
}
