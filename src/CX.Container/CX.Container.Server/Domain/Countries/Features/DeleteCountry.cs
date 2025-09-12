namespace CX.Container.Server.Domain.Countries.Features;

using CX.Container.Server.Domain.Countries.Services;
using CX.Container.Server.Services;
using MediatR;

public static class DeleteCountry
{
    public sealed record Command(Guid CountryId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(ICountryRepository countryRepository, IUnitOfWork unitOfWork)
        {
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var recordToDelete = await _countryRepository.GetById(request.CountryId, cancellationToken: cancellationToken);
            _countryRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}