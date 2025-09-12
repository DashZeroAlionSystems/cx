namespace CX.Container.Server.Domain.AgriculturalActivities.Features;

using CX.Container.Server.Domain.AgriculturalActivities.Dtos;
using CX.Container.Server.Domain.AgriculturalActivities.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetAgriculturalActivity
{
    public sealed record Query(Guid AgriculturalActivityId) : IRequest<AgriculturalActivityDto>;

    public sealed class Handler : IRequestHandler<Query, AgriculturalActivityDto>
    {
        private readonly IAgriculturalActivityRepository _agriculturalActivityRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityRepository agriculturalActivityRepository, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityRepository = agriculturalActivityRepository;
            _heimGuard = heimGuard;
        }

        public async Task<AgriculturalActivityDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivities);

            var result = await _agriculturalActivityRepository.GetById(request.AgriculturalActivityId, cancellationToken: cancellationToken);
            return result.ToAgriculturalActivityDto();
        }
    }
}