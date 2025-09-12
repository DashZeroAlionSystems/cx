namespace CX.Container.Server.Domain.Countries.Features;

using CX.Container.Server.Domain.Countries.Services;
using CX.Container.Server.Domain.Countries;
using CX.Container.Server.Domain.Countries.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddCountry
{
    public sealed record Command(CountryForCreationDto CountryToAdd) : IRequest<CountryDto>;

    public sealed class Handler : IRequestHandler<Command, CountryDto>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ICountryRepository countryRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task<CountryDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageCountries);

            var countryToAdd = request.CountryToAdd.ToCountryForCreation();
            var country = Country.Create(countryToAdd);

            await _countryRepository.Add(country, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return country.ToCountryDto();
        }
    }
}