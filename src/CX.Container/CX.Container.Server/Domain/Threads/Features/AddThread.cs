namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Domain.Threads;
using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddThread
{
    public sealed record Command(ThreadForCreationDto ThreadToAdd) : IRequest<ThreadDto>;

    public sealed class Handler : IRequestHandler<Command, ThreadDto>
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

        public async Task<ThreadDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);

            var threadToAdd = request.ThreadToAdd.ToThreadForCreation();
            var thread = Thread.Create(threadToAdd);

            await _threadRepository.Add(thread, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return thread.ToThreadDto();
        }
    }
}