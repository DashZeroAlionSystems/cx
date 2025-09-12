namespace CX.Container.Server.Domain.Preferences.Features;

using CX.Container.Server.Domain.Preferences.Services;
using CX.Container.Server.Services;
using MediatR;

public static class DeletePreference
{
    public sealed record Command(Guid PreferenceId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IPreferenceRepository _preferenceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IPreferenceRepository preferenceRepository, IUnitOfWork unitOfWork)
        {
            _preferenceRepository = preferenceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var recordToDelete = await _preferenceRepository.GetById(request.PreferenceId, cancellationToken: cancellationToken);
            _preferenceRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}