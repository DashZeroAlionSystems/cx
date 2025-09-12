namespace CX.Container.Server.Domain.Preferences.Features;

using CX.Container.Server.Domain.Preferences.Services;
using CX.Container.Server.Domain.Preferences;
using CX.Container.Server.Domain.Preferences.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddPreference
{
    public sealed record Command(PreferenceForCreationDto PreferenceToAdd) : IRequest<PreferenceDto>;

    public sealed class Handler : IRequestHandler<Command, PreferenceDto>
    {
        private readonly IPreferenceRepository _preferenceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IPreferenceRepository preferenceRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _preferenceRepository = preferenceRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task<PreferenceDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManagePreferences);

            var preferenceToAdd = request.PreferenceToAdd.ToPreferenceForCreation();
            var preference = Preference.Create(preferenceToAdd);

            await _preferenceRepository.Add(preference, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return preference.ToPreferenceDto();
        }
    }
}