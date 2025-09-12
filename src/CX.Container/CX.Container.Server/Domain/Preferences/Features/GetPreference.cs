namespace CX.Container.Server.Domain.Preferences.Features;

using CX.Container.Server.Domain.Preferences.Dtos;
using CX.Container.Server.Domain.Preferences.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetPreference
{
    public sealed record Query(Guid PreferenceId) : IRequest<PreferenceDto>;

    public sealed class Handler : IRequestHandler<Query, PreferenceDto>
    {
        private readonly IPreferenceRepository _preferenceRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IPreferenceRepository preferenceRepository, IHeimGuardClient heimGuard)
        {
            _preferenceRepository = preferenceRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PreferenceDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManagePreferences);

            var result = await _preferenceRepository.GetById(request.PreferenceId, cancellationToken: cancellationToken);
            return result.ToPreferenceDto();
        }
    }
}