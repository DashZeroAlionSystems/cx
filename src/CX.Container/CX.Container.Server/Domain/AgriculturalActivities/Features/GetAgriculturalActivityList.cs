namespace CX.Container.Server.Domain.AgriculturalActivities.Features;

using CX.Container.Server.Domain.AgriculturalActivities.Dtos;
using CX.Container.Server.Domain.AgriculturalActivities.Services;
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

public static class GetAgriculturalActivityList
{
    public sealed record Query(AgriculturalActivityParametersDto QueryParameters) : IRequest<PagedList<AgriculturalActivityDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<AgriculturalActivityDto>>
    {
        private readonly IAgriculturalActivityRepository _agriculturalActivityRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityRepository agriculturalActivityRepository, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityRepository = agriculturalActivityRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<AgriculturalActivityDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivities);

            var collection = _agriculturalActivityRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToAgriculturalActivityDtoQueryable();

            return await PagedList<AgriculturalActivityDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}