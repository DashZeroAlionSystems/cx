using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Exceptions;
using HeimGuard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Messages.Features;

public static class GetMessageRatingSummary
{
    public sealed record Query() : IRequest<IEnumerable<MessageRatingCountDto>>;

    public sealed class Handler : IRequestHandler<Query, IEnumerable<MessageRatingCountDto>>
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

        public async Task<IEnumerable<MessageRatingCountDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);

            return await _messageRepository.GetMessageRatingSummary().ToListAsync(cancellationToken);
        }
    }
}