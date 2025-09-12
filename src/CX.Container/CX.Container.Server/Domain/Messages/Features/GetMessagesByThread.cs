namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetMessagesByThread
{
    public sealed record Query(Guid ThreadId) : IRequest<List<MessageDto>>;

    public sealed class Handler : IRequestHandler<Query, List<MessageDto>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(
            IMessageRepository messageRepository,
            IHeimGuardClient heimGuard)
        {
            _messageRepository = messageRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<MessageDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);

            return await _messageRepository.GetByThreadId(request.ThreadId).ToMessageDtoQueryable()!.ToListAsync(cancellationToken);
        }
    }
}