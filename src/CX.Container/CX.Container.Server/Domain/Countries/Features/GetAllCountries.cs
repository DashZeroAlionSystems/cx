namespace CX.Container.Server.Domain.Countries.Features;

using CX.Container.Server.Domain.Countries.Dtos;
using CX.Container.Server.Domain.Countries.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllCountries
{
    public sealed record Query() : IRequest<List<CountryDto>>;

    public sealed class Handler : IRequestHandler<Query, List<CountryDto>>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ICountryRepository countryRepository, IHeimGuardClient heimGuard)
        {
            _countryRepository = countryRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<CountryDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageCountries);

            return _countryRepository.Query()
                .AsNoTracking()
                .ToCountryDtoQueryable()
                .ToList();
        }
    }
}