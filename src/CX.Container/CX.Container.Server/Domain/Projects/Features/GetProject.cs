namespace CX.Container.Server.Domain.Projects.Features;

using CX.Container.Server.Domain.Projects.Dtos;
using CX.Container.Server.Domain.Projects.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class GetProject
{
    public sealed record Query(Guid ProjectId) : IRequest<ProjectDto>;

    public sealed class Handler : IRequestHandler<Query, ProjectDto>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProjectRepository projectRepository, IHeimGuardClient heimGuard)
        {
            _projectRepository = projectRepository;
            _heimGuard = heimGuard;
        }

        public async Task<ProjectDto> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProjects);

            var result = await _projectRepository.GetById(request.ProjectId, cancellationToken: cancellationToken);
            return result.ToProjectDto();
        }
    }
}