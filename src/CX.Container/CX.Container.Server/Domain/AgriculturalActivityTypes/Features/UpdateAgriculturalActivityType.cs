namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Features;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateAgriculturalActivityType
{
    public sealed record Command(Guid AgriculturalActivityTypeId, AgriculturalActivityTypeForUpdateDto UpdatedAgriculturalActivityTypeData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IAgriculturalActivityTypeRepository _agriculturalActivityTypeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityTypeRepository agriculturalActivityTypeRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityTypeRepository = agriculturalActivityTypeRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivityTypes);

            var agriculturalActivityTypeToUpdate = await _agriculturalActivityTypeRepository.GetById(request.AgriculturalActivityTypeId, cancellationToken: cancellationToken);
            var agriculturalActivityTypeToAdd = request.UpdatedAgriculturalActivityTypeData.ToAgriculturalActivityTypeForUpdate();
            agriculturalActivityTypeToUpdate.Update(agriculturalActivityTypeToAdd);

            _agriculturalActivityTypeRepository.Update(agriculturalActivityTypeToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}