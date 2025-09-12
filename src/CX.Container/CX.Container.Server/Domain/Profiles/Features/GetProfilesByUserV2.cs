using CX.Container.Server.Services;

namespace CX.Container.Server.Domain.Profiles.Features;

using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Domain.Profiles.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetProfilesByUserV2
{
    public sealed record Query() : IRequest<List<ProfileDtoV2>>;

    public sealed class Handler : IRequestHandler<Query, List<ProfileDtoV2>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IProfileRepository _profileRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(
            ICurrentUserService currentUserService,
            IProfileRepository profileRepository,
            IHeimGuardClient heimGuard)
        {
            _currentUserService = currentUserService;
            _profileRepository = profileRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<ProfileDtoV2>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);
            
            if (_currentUserService.UserId is null) throw new ForbiddenAccessException("User is not authenticated.");
            var userId = _currentUserService.UserId;

            return await _profileRepository
                .Query()
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.IsDeleted == false)
                .ToProfileDtoQueryableV2()
                .ToListAsync(cancellationToken);
        }
    }
}