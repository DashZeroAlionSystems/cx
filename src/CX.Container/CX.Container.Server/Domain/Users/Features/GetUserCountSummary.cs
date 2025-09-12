using CX.Container.Server.Domain.Users.Dtos;
using CX.Container.Server.Domain.Users.Services;
using CX.Container.Server.Exceptions;
using HeimGuard;
using MediatR;

namespace CX.Container.Server.Domain.Users.Features;

public static class GetUserCountSummary
{
    public sealed record Query() : IRequest<IEnumerable<CreatedUserSummaryDto>>;

    public sealed class Handler : IRequestHandler<Query, IEnumerable<CreatedUserSummaryDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IUserRepository userRepository, IHeimGuardClient heimGuard)
        {
            _userRepository = userRepository;
            _heimGuard = heimGuard;
        }

        public async Task<IEnumerable<CreatedUserSummaryDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = await _userRepository.GetUserCountOverTimeAsync(cancellationToken);
            return FillMissingMonths(query);
        }
        
        private static List<CreatedUserSummaryDto> FillMissingMonths(List<CreatedUserSummaryDto> data, int monthsToShow = 6)
        {
            if (data == null || data.Count == 0)
                return [];

            var filledData = new List<CreatedUserSummaryDto>();
            var latestDate = data.Max(d => new DateTime(d.Year, d.Month, 1));
            var startDate = latestDate.AddMonths(-monthsToShow + 1);

            for (var date = startDate; date <= latestDate; date = date.AddMonths(1))
            {
                var existingData = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
                filledData.Add(existingData ?? new CreatedUserSummaryDto
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