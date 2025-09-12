using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Users.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Users.Features;

using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;

public static class DeleteUser
{
    public sealed record Command(string UserId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ILogger<Handler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;
        private readonly ICurrentUserService _currentUserService;

        public Handler(
            IUnitOfWork unitOfWork,
            IHeimGuardClient heimGuard,
            ICurrentUserService currentUserService,
            ILogger<Handler> logger)
        {
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanDeleteUsers);

            var userId = request.UserId
                         ?? _currentUserService.UserId
                         ?? throw new ForbiddenAccessException("User is not authenticated.");

            await _unitOfWork.WrapInTransactionAsync(async (db, ct) =>
            {
                await DeleteUser(db, ct);
            }, cancellationToken);
            

            // local method
            async Task DeleteUser(AelaDbContext db, CancellationToken ct)
            {
                try
                {
                    db.QueueDomainEvent(new UserDeleted { Id = userId });

                    await db.AgriculturalActivities.Where(a => a.CreatedBy == userId).ExecuteDeleteAsync(ct);
                    await db.Preferences.Where(p => p.CreatedBy == userId).ExecuteDeleteAsync(ct);
                    await db.Profiles.Where(p => p.CreatedBy == userId).ExecuteDeleteAsync(ct);
                    await db.Messages.Where(m => m.CreatedBy == userId).ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDeleted, true));
                    await db.Threads.Where(t => t.CreatedBy == userId).ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDeleted, true));
                    await db.UserRoles.Where(ur => ur.CreatedBy == userId).ExecuteDeleteAsync(ct);
                    await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(ct);

                    _logger.LogInformation("Deleted user {UserId}", userId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Delete of user {UserId} failed", userId);
                    throw;
                }
            }
        }
    }
}