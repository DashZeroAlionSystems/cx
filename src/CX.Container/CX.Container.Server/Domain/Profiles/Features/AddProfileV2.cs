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

public static class AddProfileV2
{
    public sealed record Command(ProfileForCreationDtoV2 ProfileToAdd) : IRequest<ProfileDtoV2>;

    public sealed class Handler : IRequestHandler<Command, ProfileDtoV2>
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

        public async Task<ProfileDtoV2> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);

            if (string.IsNullOrWhiteSpace(request.ProfileToAdd.UserId))
                request.ProfileToAdd.UserId = _currentUserService.UserId;
            
            var profileToAdd = request.ProfileToAdd.ToProfileForCreation();
            var profile = Profile.CreateV2(profileToAdd);
            
            await _profileRepository.Add(profile, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return profile.ToProfileDtoV2();
        }
    }
}