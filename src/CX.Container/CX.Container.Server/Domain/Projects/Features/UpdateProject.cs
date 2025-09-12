namespace CX.Container.Server.Domain.Projects.Features;

using CX.Container.Server.Domain.Projects.Dtos;
using CX.Container.Server.Domain.Projects.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateProject
{
    public sealed record Command(Guid ProjectId, ProjectForUpdateDto UpdatedProjectData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
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

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProjects);

            var projectToUpdate = await _projectRepository.GetById(request.ProjectId, cancellationToken: cancellationToken);
            var projectToAdd = request.UpdatedProjectData.ToProjectForUpdate();
            projectToUpdate.Update(projectToAdd);
            projectToAdd.Namespace = projectToUpdate.Namespace;

            _projectRepository.Update(projectToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}