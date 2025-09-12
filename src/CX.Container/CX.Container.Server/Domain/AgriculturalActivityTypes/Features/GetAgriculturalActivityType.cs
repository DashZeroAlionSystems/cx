namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Features;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetAgriculturalActivityType
{
    public sealed record Query(Guid AgriculturalActivityTypeId) : IRequest<AgriculturalActivityTypeDto>;

    public sealed class Handler : IRequestHandler<Query, AgriculturalActivityTypeDto>
    {
        private readonly IAgriculturalActivityTypeRepository _agriculturalActivityTypeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityTypeRepository agriculturalActivityTypeRepository, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityTypeRepository = agriculturalActivityTypeRepository;
            _heimGuard = heimGuard;
        }

        public async Task<AgriculturalActivityTypeDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivityTypes);

            var result = await _agriculturalActivityTypeRepository.GetById(request.AgriculturalActivityTypeId, cancellationToken: cancellationToken);
            return result.ToAgriculturalActivityTypeDto();
        }
    }
}