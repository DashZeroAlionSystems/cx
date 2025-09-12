namespace CX.Container.Server.Domain.Users.Features;

using CX.Container.Server.Domain.Users.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using HeimGuard;
using MediatR;
using Roles;

public static class AddUserRole
{
    public sealed record Command(string UserId, string Role, bool SkipPermissions = false) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IUserRepository userRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            if(!request.SkipPermissions)
                await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanAddUserRoles);
            
            var user = await _userRepository.GetById(request.UserId, true, cancellationToken);

            if(!_userRepository.GetRolesByUserIdentifier(request.UserId).Contains(request.Role))
            {
                var roleToAdd = user.AddRole(Role.Of(request.Role));
                await _userRepository.AddRole(roleToAdd, cancellationToken);
                await _unitOfWork.CommitChanges(cancellationToken);

                // Expires the user's permission cache
                await _heimGuard.HasPermissionAsync($"{Permissions.ClearPermissionCache}{request.UserId}");
            }            
        }
    }
}