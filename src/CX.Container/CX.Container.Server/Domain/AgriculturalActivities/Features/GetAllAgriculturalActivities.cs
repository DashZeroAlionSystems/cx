namespace CX.Container.Server.Domain.AgriculturalActivities.Features;

using CX.Container.Server.Domain.AgriculturalActivities.Dtos;
using CX.Container.Server.Domain.AgriculturalActivities.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllAgriculturalActivities
{
    public sealed record Query() : IRequest<List<AgriculturalActivityDto>>;

    public sealed class Handler : IRequestHandler<Query, List<AgriculturalActivityDto>>
    {
        private readonly IAgriculturalActivityRepository _agriculturalActivityRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IAgriculturalActivityRepository agriculturalActivityRepository, IHeimGuardClient heimGuard)
        {
            _agriculturalActivityRepository = agriculturalActivityRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<AgriculturalActivityDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageAgriculturalActivities);

            return _agriculturalActivityRepository.Query()
                .AsNoTracking()
                .ToAgriculturalActivityDtoQueryable()
                .ToList();
        }
    }
}