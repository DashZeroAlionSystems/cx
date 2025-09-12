namespace CX.Container.Server.Domain.AgriculturalActivities.Features;

using CX.Container.Server.Domain.AgriculturalActivities.Dtos;
using CX.Container.Server.Domain.AgriculturalActivities.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateAgriculturalActivity
{
    public sealed record Command(Guid AgriculturalActivityId, AgriculturalActivityForUpdateDto UpdatedAgriculturalActivityData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
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

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivities);

            var agriculturalActivityToUpdate = await _agriculturalActivityRepository.GetById(request.AgriculturalActivityId, cancellationToken: cancellationToken);
            var agriculturalActivityToAdd = request.UpdatedAgriculturalActivityData.ToAgriculturalActivityForUpdate();
            agriculturalActivityToUpdate.Update(agriculturalActivityToAdd);

            _agriculturalActivityRepository.Update(agriculturalActivityToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}