namespace CX.Container.Server.Domain.AgriculturalActivities.Features;

using CX.Container.Server.Domain.AgriculturalActivities.Services;
using CX.Container.Server.Domain.AgriculturalActivities;
using CX.Container.Server.Domain.AgriculturalActivities.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddAgriculturalActivity
{
    public sealed record Command(AgriculturalActivityForCreationDto AgriculturalActivityToAdd) : IRequest<AgriculturalActivityDto>;

    public sealed class Handler : IRequestHandler<Command, AgriculturalActivityDto>
    {
        private readonly IAgriculturalActivityRepository _agriculturalActivityRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityRepository agriculturalActivityRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityRepository = agriculturalActivityRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task<AgriculturalActivityDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivities);

            var agriculturalActivityToAdd = request.AgriculturalActivityToAdd.ToAgriculturalActivityForCreation();
            var agriculturalActivity = AgriculturalActivity.Create(agriculturalActivityToAdd);

            await _agriculturalActivityRepository.Add(agriculturalActivity, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return agriculturalActivity.ToAgriculturalActivityDto();
        }
    }
}