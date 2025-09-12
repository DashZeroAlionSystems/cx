using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Exceptions;
using HeimGuard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Messages.Features;

public static class GetMessageSummary
{
    public sealed record Query() : IRequest<IEnumerable<MessageCountDto>>;

    public sealed class Handler : IRequestHandler<Query, IEnumerable<MessageCountDto>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(
            IMessageRepository messageRepository,
            IHeimGuardClient heimGuard
        )
        {
            _messageRepository = messageRepository;
            _heimGuard = heimGuard;
        }

        public async Task<IEnumerable<MessageCountDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);
            var query = await _messageRepository.GetMessageSummary().ToListAsync(cancellationToken);
            return FillMissingMonths(query);
        }
        
        private static List<MessageCountDto> FillMissingMonths(List<MessageCountDto> data, int monthsToShow = 6)
        {
            if (data == null || data.Count == 0)
                return [];

            var filledData = new List<MessageCountDto>();
            var latestDate = data.Max(d => new DateTime(d.Year, d.Month, 1));
            var startDate = latestDate.AddMonths(-monthsToShow + 1);

            for (var date = startDate; date <= latestDate; date = date.AddMonths(1))
            {
                var existingData = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
                filledData.Add(existingData ?? new MessageCountDto
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