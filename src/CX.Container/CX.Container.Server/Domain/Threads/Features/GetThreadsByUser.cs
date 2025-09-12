using CX.Container.Server.Services;

namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetThreadsByUser
{
    public sealed record Query() : IRequest<List<ThreadDto>>;

    public sealed class Handler : IRequestHandler<Query, List<ThreadDto>>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IHeimGuardClient _heimGuard;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IThreadRepository threadRepository, IHeimGuardClient heimGuard, ICurrentUserService currentUserService)
        {
            _threadRepository = threadRepository;
            _heimGuard = heimGuard;
            _currentUserService = currentUserService;
        }

        public async Task<List<ThreadDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);
            
            if (_currentUserService.UserId is null) throw new ForbiddenAccessException("User is not authenticated.");
            
            var userId = _currentUserService.UserId;

            return await _threadRepository
                .Query()
                .AsNoTracking()
                .Where(t => t.CreatedBy == userId && t.IsDeleted == false)
                .OrderBy(t => t.CreatedOn)
                .Select(t => t.ToThreadDto())
                .ToListAsync(cancellationToken);
        }
    }
}