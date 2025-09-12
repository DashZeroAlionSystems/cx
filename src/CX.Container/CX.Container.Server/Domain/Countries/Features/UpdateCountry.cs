namespace CX.Container.Server.Domain.Countries.Features;

using CX.Container.Server.Domain.Countries.Dtos;
using CX.Container.Server.Domain.Countries.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateCountry
{
    public sealed record Command(Guid CountryId, CountryForUpdateDto UpdatedCountryData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
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

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageCountries);

            var countryToUpdate = await _countryRepository.GetById(request.CountryId, cancellationToken: cancellationToken);
            var countryToAdd = request.UpdatedCountryData.ToCountryForUpdate();
            countryToUpdate.Update(countryToAdd);

            _countryRepository.Update(countryToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}