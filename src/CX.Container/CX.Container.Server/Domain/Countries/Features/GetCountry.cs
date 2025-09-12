namespace CX.Container.Server.Domain.Countries.Features;

using CX.Container.Server.Domain.Countries.Dtos;
using CX.Container.Server.Domain.Countries.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetCountry
{
    public sealed record Query(Guid CountryId) : IRequest<CountryDto>;

    public sealed class Handler : IRequestHandler<Query, CountryDto>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ICountryRepository countryRepository, IHeimGuardClient heimGuard)
        {
            _countryRepository = countryRepository;
            _heimGuard = heimGuard;
        }

        public async Task<CountryDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageCountries);

            var result = await _countryRepository.GetById(request.CountryId, cancellationToken: cancellationToken);
            return result.ToCountryDto();
        }
    }
}