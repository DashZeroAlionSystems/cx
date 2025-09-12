namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetMessage
{
    public sealed record Query(Guid MessageId) : IRequest<MessageDto>;

    public sealed class Handler : IRequestHandler<Query, MessageDto>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IMessageRepository messageRepository, IHeimGuardClient heimGuard)
        {
            _messageRepository = messageRepository;
            _heimGuard = heimGuard;
        }

        public async Task<MessageDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);

            var result = await _messageRepository.GetById(request.MessageId, cancellationToken: cancellationToken);
            return result.ToMessageDto();
        }
    }
}