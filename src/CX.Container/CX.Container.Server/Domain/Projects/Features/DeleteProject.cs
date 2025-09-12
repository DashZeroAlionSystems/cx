namespace CX.Container.Server.Domain.Projects.Features;

using CX.Container.Server.Domain.Projects.Services;
using CX.Container.Server.Services;
using MediatR;

public static class DeleteProject
{
    public sealed record Command(Guid ProjectId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
        {
            _projectRepository = projectRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var recordToDelete = await _projectRepository.GetById(request.ProjectId, cancellationToken: cancellationToken);
            _projectRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}