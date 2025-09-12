namespace CX.Container.Server.Domain.Projects.Features;

using CX.Container.Server.Domain.Projects.Dtos;
using CX.Container.Server.Domain.Projects.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetAllProjects
{
    public sealed record Query() : IRequest<List<ProjectDto>>;

    public sealed class Handler : IRequestHandler<Query, List<ProjectDto>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProjectRepository projectRepository, IHeimGuardClient heimGuard)
        {
            _projectRepository = projectRepository;
            _heimGuard = heimGuard;
        }

        public async Task<List<ProjectDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProjects);

            return _projectRepository.Query()
                .AsNoTracking()
                .ToProjectDtoQueryable()
                .ToList();
        }
    }
}