namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Features;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllAgriculturalActivityTypes
{
    public sealed record Query() : IRequest<List<AgriculturalActivityTypeDto>>;

    public sealed class Handler : IRequestHandler<Query, List<AgriculturalActivityTypeDto>>
    {
        private readonly IAgriculturalActivityTypeRepository _agriculturalActivityTypeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityTypeRepository agriculturalActivityTypeRepository, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityTypeRepository = agriculturalActivityTypeRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<AgriculturalActivityTypeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivityTypes);

            return _agriculturalActivityTypeRepository.Query()
                .AsNoTracking()
                .ToAgriculturalActivityTypeDtoQueryable()
                .ToList();
        }
    }
}