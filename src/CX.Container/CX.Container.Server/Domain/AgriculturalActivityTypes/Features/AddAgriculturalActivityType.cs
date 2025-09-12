namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Features;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Services;
using CX.Container.Server.Domain.AgriculturalActivityTypes;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddAgriculturalActivityType
{
    public sealed record Command(AgriculturalActivityTypeForCreationDto AgriculturalActivityTypeToAdd) : IRequest<AgriculturalActivityTypeDto>;

    public sealed class Handler : IRequestHandler<Command, AgriculturalActivityTypeDto>
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

        public async Task<AgriculturalActivityTypeDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivityTypes);

            var agriculturalActivityTypeToAdd = request.AgriculturalActivityTypeToAdd.ToAgriculturalActivityTypeForCreation();
            var agriculturalActivityType = AgriculturalActivityType.Create(agriculturalActivityTypeToAdd);

            await _agriculturalActivityTypeRepository.Add(agriculturalActivityType, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return agriculturalActivityType.ToAgriculturalActivityTypeDto();
        }
    }
}