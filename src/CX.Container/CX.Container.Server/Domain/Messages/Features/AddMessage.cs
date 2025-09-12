namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Domain.Messages;
using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddMessage
{
    public sealed record Command(MessageForCreationDto MessageToAdd) : IRequest<MessageDto>;

    public sealed class Handler : IRequestHandler<Command, MessageDto>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IMessageRepository messageRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task<MessageDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);

            var messageToAdd = request.MessageToAdd.ToMessageForCreation();
            var message = Message.Create(messageToAdd);

            await _messageRepository.Add(message, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return message.ToMessageDto();
        }
    }
}