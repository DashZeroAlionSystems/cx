namespace CX.Container.Server.Domain.Profiles.Features;

using CX.Container.Server.Domain.Profiles.Services;
using CX.Container.Server.Domain.Profiles;
using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddProfile
{
    /// <summary>
    /// Command to add a profile.
    /// </summary>
    public sealed record Command(ProfileForCreationDto ProfileToAdd) : IRequest<ProfileDto>;

    public sealed class Handler : IRequestHandler<Command, ProfileDto>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(
            IProfileRepository profileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            IHeimGuardClient heimGuard)
        {
            _profileRepository = profileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        /// <summary>
        /// Handles the command to add a profile.
        /// </summary>
        /// <param name="request">The command request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The added profile.</returns>
        public async Task<ProfileDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);

            if (string.IsNullOrWhiteSpace(request.ProfileToAdd.UserId))
                request.ProfileToAdd.UserId = _currentUserService.UserId;

            // Default Location
            if (string.IsNullOrWhiteSpace(request.ProfileToAdd.LocationId))
            {
                request.ProfileToAdd.LocationId = "ChIJJ0uV9Q17lR4RfyJicLptT0c";
                request.ProfileToAdd.Longitude = "28.1127491";
                request.ProfileToAdd.Latitude = "-25.8347475";
                request.ProfileToAdd.Name = "Centurion";
            }

            var profileToAdd = request.ProfileToAdd.ToProfileForCreation();
            var profile = Profile.Create(profileToAdd);

            await _profileRepository.Add(profile, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return profile.ToProfileDto();
        }
    }
}
