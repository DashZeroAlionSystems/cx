namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateThread
{
    public sealed record Command(Guid ThreadId, ThreadForUpdateDto UpdatedThreadData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IThreadRepository threadRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _threadRepository = threadRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);

            var threadToUpdate = await _threadRepository.GetById(request.ThreadId, cancellationToken: cancellationToken);
            var threadToAdd = request.UpdatedThreadData.ToThreadForUpdate();
            threadToUpdate.Update(threadToAdd);

            _threadRepository.Update(threadToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}