namespace CX.Container.Server.Domain.Sources.Features;

using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Domain.Sources.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateSource
{
    public sealed record Command(Guid SourceId, SourceForUpdateDto UpdatedSourceData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ISourceRepository _sourceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceRepository sourceRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard)
        {
            _sourceRepository = sourceRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSources);

            var sourceToUpdate = await _sourceRepository.GetById(request.SourceId, cancellationToken: cancellationToken);
            var sourceToAdd = request.UpdatedSourceData.ToSourceForUpdate();
            sourceToUpdate.Update(sourceToAdd);

            _sourceRepository.Update(sourceToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}