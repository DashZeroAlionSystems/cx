namespace CX.Container.Server.Domain.Profiles.Features;

using CX.Container.Server.Domain.Profiles.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;

public static class DeleteProfile
{
    public sealed record Command(Guid ProfileId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProfileRepository profileRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _profileRepository = profileRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProfiles);

            var recordToDelete = await _profileRepository.GetById(request.ProfileId, cancellationToken: cancellationToken);
            _profileRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}