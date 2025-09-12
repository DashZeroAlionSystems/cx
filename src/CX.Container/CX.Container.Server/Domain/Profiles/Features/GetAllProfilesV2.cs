namespace CX.Container.Server.Domain.Profiles.Features;

using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Domain.Profiles.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllProfilesV2
{
    public sealed record Query() : IRequest<List<ProfileDtoV2>>;

    public sealed class Handler : IRequestHandler<Query, List<ProfileDtoV2>>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProfileRepository profileRepository, IHeimGuardClient heimGuard)
        {
            _profileRepository = profileRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<ProfileDtoV2>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);

            return _profileRepository.Query()
                .AsNoTracking()
                .ToProfileDtoQueryableV2()
                .ToList();
        }
    }
}