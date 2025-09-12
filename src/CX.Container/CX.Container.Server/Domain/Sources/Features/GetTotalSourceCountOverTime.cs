using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Domain.Sources.Services;
using MediatR;

namespace CX.Container.Server.Domain.Sources.Features;

public static class GetTotalSourceCountOverTime
{
    public sealed record Query() : IRequest<IEnumerable<SourceCountDto>>;

    public sealed class Handler : IRequestHandler<Query, IEnumerable<SourceCountDto>>
    {
        private readonly ISourceRepository _sourceRepository;

        public Handler(ISourceRepository sourceRepository)
        {
            _sourceRepository = sourceRepository;
        }

        public async Task<IEnumerable<SourceCountDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = await _sourceRepository.GetTotalCountOverTimeAsync(cancellationToken);
            return FillMissingMonths(query);
        }

        private static List<SourceCountDto> FillMissingMonths(List<SourceCountDto> data, int monthsToShow = 6)
        {
            if (data == null || data.Count == 0)
                return [];

            var filledData = new List<SourceCountDto>();
            var latestDate = data.Max(d => new DateTime(d.Year, d.Month, 1));
            var startDate = latestDate.AddMonths(-monthsToShow + 1);

            for (var date = startDate; date <= latestDate; date = date.AddMonths(1))
            {
                var existingData = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
                filledData.Add(existingData ?? new SourceCountDto
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