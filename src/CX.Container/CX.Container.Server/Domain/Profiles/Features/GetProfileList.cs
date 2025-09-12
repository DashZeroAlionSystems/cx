namespace CX.Container.Server.Domain.Profiles.Features;

using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Domain.Profiles.Services;
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

public static class GetProfileList
{
    public sealed record Query(ProfileParametersDto QueryParameters) : IRequest<PagedList<ProfileDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<ProfileDto>>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProfileRepository profileRepository, IHeimGuardClient heimGuard)
        {
            _profileRepository = profileRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<ProfileDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);

            var collection = _profileRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToProfileDtoQueryable();

            return await PagedList<ProfileDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}