namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetThread
{
    public sealed record Query(Guid ThreadId) : IRequest<ThreadDto>;

    public sealed class Handler : IRequestHandler<Query, ThreadDto>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IThreadRepository threadRepository, IHeimGuardClient heimGuard)
        {
            _threadRepository = threadRepository;
            _heimGuard = heimGuard;
        }

        public async Task<ThreadDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);

            var result = await _threadRepository.GetById(request.ThreadId, cancellationToken: cancellationToken);
            return result.ToThreadDto();
        }
    }
}