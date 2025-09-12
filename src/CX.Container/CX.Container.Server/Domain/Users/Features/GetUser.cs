namespace CX.Container.Server.Domain.Users.Features;

using CX.Container.Server.Domain.Users.Dtos;
using CX.Container.Server.Domain.Users.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetUser
{
    public sealed record Query(string UserId) : IRequest<UserDto>;

    public sealed class Handler : IRequestHandler<Query, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IUserRepository userRepository, IHeimGuardClient heimGuard)
        {
            _userRepository = userRepository;
            _heimGuard = heimGuard;
        }

        public async Task<UserDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanReadUsers);

            var result = await _userRepository.GetById(request.UserId, cancellationToken: cancellationToken);
            var roles = result.Roles?.Select(x => x.Role.Value)?.ToList() ?? new List<string>();

            return new UserDto
            {
                Email = result.Email,
                Username = result.Username,
                Roles = roles,
                FirstName = result.FirstName,
                LastName = result.LastName,
                Id = result.Id
            };
        }
    }
}