namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllThreads
{
    public sealed record Query() : IRequest<List<ThreadDto>>;

    public sealed class Handler : IRequestHandler<Query, List<ThreadDto>>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IThreadRepository threadRepository, IHeimGuardClient heimGuard)
        {
            _threadRepository = threadRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<ThreadDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);

            return _threadRepository.Query()
                .AsNoTracking()
                .ToThreadDtoQueryable()
                .ToList();
        }
    }
}