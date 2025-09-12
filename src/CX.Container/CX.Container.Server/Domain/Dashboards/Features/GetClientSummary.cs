namespace CX.Container.Server.Domain.Dashboards.Features;

using CX.Container.Server.Domain.Dashboards.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Services;
using MediatR;

public static class GetClientSummary
{
    public sealed record Query() : IRequest<ClientSummaryDto>;

    public sealed class Handler : IRequestHandler<Query, ClientSummaryDto>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        
        public Handler(ISourceDocumentRepository sourceDocumentRepository)
        {
            _sourceDocumentRepository = sourceDocumentRepository;            
        }

        public async Task<ClientSummaryDto> Handle(Query request, CancellationToken cancellationToken)
        {
            var clientSummaryDto = await _sourceDocumentRepository.GetClientSummary(cancellationToken);
            return clientSummaryDto;
        }
    }
}