namespace CX.Container.Server.Domain.Countries.Features;

using CX.Container.Server.Domain.Countries.Dtos;
using CX.Container.Server.Domain.Countries.Services;
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

public static class GetCountryList
{
    public sealed record Query(CountryParametersDto QueryParameters) : IRequest<PagedList<CountryDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<CountryDto>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ICountryRepository countryRepository, IHeimGuardClient heimGuard)
        {
            _countryRepository = countryRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<CountryDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageCountries);

            var collection = _countryRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToCountryDtoQueryable();

            return await PagedList<CountryDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}