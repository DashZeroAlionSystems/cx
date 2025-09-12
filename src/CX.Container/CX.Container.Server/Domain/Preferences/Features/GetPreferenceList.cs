namespace CX.Container.Server.Domain.Preferences.Features;

using CX.Container.Server.Domain.Preferences.Dtos;
using CX.Container.Server.Domain.Preferences.Services;
using CX.Container.Server.Wrappers;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Resources;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;
using QueryKit;
using QueryKit.Configuration;

public static class GetPreferenceList
{
    public sealed record Query(PreferenceParametersDto QueryParameters) : IRequest<PagedList<PreferenceDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<PreferenceDto>>
    {
        private readonly IPreferenceRepository _preferenceRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IPreferenceRepository preferenceRepository, IHeimGuardClient heimGuard)
        {
            _preferenceRepository = preferenceRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<PreferenceDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManagePreferences);

            var collection = _preferenceRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToPreferenceDtoQueryable();

            return await PagedList<PreferenceDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}