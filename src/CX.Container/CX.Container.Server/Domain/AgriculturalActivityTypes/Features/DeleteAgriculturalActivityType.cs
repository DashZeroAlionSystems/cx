using HeimGuard;

namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Features;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using MediatR;

public static class DeleteAgriculturalActivityType
{
    public sealed record Command(Guid AgriculturalActivityTypeId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IAgriculturalActivityTypeRepository _agriculturalActivityTypeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(
            IAgriculturalActivityTypeRepository agriculturalActivityTypeRepository,
            IUnitOfWork unitOfWork,
            IHeimGuardClient heimGuard)
        {
            _agriculturalActivityTypeRepository = agriculturalActivityTypeRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivityTypes);
            
            var recordToDelete = await _agriculturalActivityTypeRepository.GetById(request.AgriculturalActivityTypeId, cancellationToken: cancellationToken);
            _agriculturalActivityTypeRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}