namespace CX.Container.Server.Domain.Projects.Features;

using CX.Container.Server.Domain.Projects.Services;
using CX.Container.Server.Domain.Projects;
using CX.Container.Server.Domain.Projects.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddProject
{
	public sealed record Command(ProjectForCreationDto ProjectToAdd) : IRequest<ProjectDto>;

	public sealed class Handler : IRequestHandler<Command, ProjectDto>
	{
		private readonly IProjectRepository _projectRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IHeimGuardClient _heimGuard;

		public Handler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
		{
			_projectRepository = projectRepository;
			_unitOfWork = unitOfWork;
			_heimGuard = heimGuard;
		}

		public async Task<ProjectDto> Handle(Command request, CancellationToken cancellationToken)
		{
			await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProjects);
			var projectToAdd = request.ProjectToAdd.ToProjectForCreation();
			var project = Project.Create(projectToAdd);
			await _projectRepository.Add(project, cancellationToken);
			await _unitOfWork.CommitChanges(cancellationToken);
			return project.ToProjectDto();
		}
	}
}