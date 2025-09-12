namespace CX.Container.Server.Domain.Profiles.Features;

using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Domain.Profiles.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetProfileV2
{
    public sealed record Query(Guid ProfileId) : IRequest<ProfileDtoV2>;

    public sealed class Handler : IRequestHandler<Query, ProfileDtoV2>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProfileRepository profileRepository, IHeimGuardClient heimGuard)
        {
            _profileRepository = profileRepository;
            _heimGuard = heimGuard;
        }

        public async Task<ProfileDtoV2> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);

            var result = await _profileRepository.GetById(request.ProfileId, cancellationToken: cancellationToken);
            return result.ToProfileDtoV2();
        }
    }
}