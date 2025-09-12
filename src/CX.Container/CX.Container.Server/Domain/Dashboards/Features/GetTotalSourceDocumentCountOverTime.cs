namespace CX.Container.Server.Domain.Dashboards.Features;

using CX.Container.Server.Domain.Dashboards.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Services;
using MediatR;

public static class GetTotalSourceDocumentCountOverTime
{
    public sealed record Query() : IRequest<IEnumerable<DocumentCountDto>>;

    public sealed class Handler : IRequestHandler<Query, IEnumerable<DocumentCountDto>>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        
        public Handler(ISourceDocumentRepository sourceDocumentRepository)
        {
            _sourceDocumentRepository = sourceDocumentRepository;            
        }

        public async Task<IEnumerable<DocumentCountDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = await _sourceDocumentRepository.GetTotalCountOverTimeAsync(cancellationToken);
            return FillMissingMonths(query);
        }
        
        public static List<DocumentCountDto> FillMissingMonths(List<DocumentCountDto> data, int monthsToShow = 6)
        {
            if (data == null || data.Count == 0)
                return [];

            var filledData = new List<DocumentCountDto>();
            var latestDate = data.Max(d => new DateTime(d.Year, d.Month, 1));
            var startDate = latestDate.AddMonths(-monthsToShow + 1);

            for (var date = startDate; date <= latestDate; date = date.AddMonths(1))
            {
                var existingData = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
                filledData.Add(existingData ?? new DocumentCountDto
                {
                    TotalCount = 0,
                    Month = date.Month,
                    Year = date.Year
                });
            }

            return filledData;
        }
    }
}