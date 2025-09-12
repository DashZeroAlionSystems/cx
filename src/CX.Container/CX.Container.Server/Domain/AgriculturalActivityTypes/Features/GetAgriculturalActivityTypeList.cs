namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Features;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Services;
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

public static class GetAgriculturalActivityTypeList
{
    public sealed record Query(AgriculturalActivityTypeParametersDto QueryParameters) : IRequest<PagedList<AgriculturalActivityTypeDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<AgriculturalActivityTypeDto>>
    {
        private readonly IAgriculturalActivityTypeRepository _agriculturalActivityTypeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityTypeRepository agriculturalActivityTypeRepository, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityTypeRepository = agriculturalActivityTypeRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<AgriculturalActivityTypeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivityTypes);

            var collection = _agriculturalActivityTypeRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToAgriculturalActivityTypeDtoQueryable();

            return await PagedList<AgriculturalActivityTypeDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}