using CX.Container.Server.Domain.Users.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Services;
using HeimGuard;
using MediatR;

namespace CX.Container.Server.Domain.Users.Features;

public static class GetMyRoles
{
    public sealed record Query(string UserId) : IRequest<IEnumerable<string>>;

    public sealed class Handler : IRequestHandler<Query, IEnumerable<string>>
    {
        private readonly IHeimGuardClient _heimGuard;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;

        public Handler(
            IHeimGuardClient heimGuard,
            ICurrentUserService currentUserService,
            IUserRepository userRepository)
        {
            _heimGuard = heimGuard;
            _currentUserService = currentUserService;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<string>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanReadUsers);

            return _userRepository.GetRolesByUserIdentifier(_currentUserService.UserId);
        }
    }
}