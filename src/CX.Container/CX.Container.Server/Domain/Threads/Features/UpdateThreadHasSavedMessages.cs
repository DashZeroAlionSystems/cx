namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;
using CX.Container.Server.Domain.Messages.Services;

public static class UpdateThreadHasSavedMessages
{
    public sealed record Command(Guid MessageId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IThreadRepository threadRepository, IMessageRepository messageRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _threadRepository = threadRepository;
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);

            var messageToUpdate = await _messageRepository.GetById(request.MessageId, cancellationToken: cancellationToken);
            var threadToUpdate = await _threadRepository.GetById((Guid)messageToUpdate.ThreadId, cancellationToken: cancellationToken);
            threadToUpdate.UpdateHasPinnedMessages();

            _threadRepository.Update(threadToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}