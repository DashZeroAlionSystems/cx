namespace CX.Container.Server.Domain.Preferences.Features;

using CX.Container.Server.Domain.Preferences.Dtos;
using CX.Container.Server.Domain.Preferences.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdatePreference
{
    public sealed record Command(Guid PreferenceId, PreferenceForUpdateDto UpdatedPreferenceData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
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

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManagePreferences);

            var preferenceToUpdate = await _preferenceRepository.GetById(request.PreferenceId, cancellationToken: cancellationToken);
            var preferenceToAdd = request.UpdatedPreferenceData.ToPreferenceForUpdate();
            preferenceToUpdate.Update(preferenceToAdd);

            _preferenceRepository.Update(preferenceToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}