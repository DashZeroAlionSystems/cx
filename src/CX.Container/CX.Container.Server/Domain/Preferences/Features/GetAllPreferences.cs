namespace CX.Container.Server.Domain.Preferences.Features;

using CX.Container.Server.Domain.Preferences.Dtos;
using CX.Container.Server.Domain.Preferences.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllPreferences
{
    public sealed record Query() : IRequest<List<PreferenceDto>>;

    public sealed class Handler : IRequestHandler<Query, List<PreferenceDto>>
    {
        private readonly IPreferenceRepository _preferenceRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IPreferenceRepository preferenceRepository, IHeimGuardClient heimGuard)
        {
            _preferenceRepository = preferenceRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<PreferenceDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManagePreferences);

            return _preferenceRepository.Query()
                .AsNoTracking()
                .ToPreferenceDtoQueryable()
                .ToList();
        }
    }
}