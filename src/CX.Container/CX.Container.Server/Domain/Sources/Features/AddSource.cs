namespace CX.Container.Server.Domain.Sources.Features;

using CX.Container.Server.Domain.Sources.Services;
using CX.Container.Server.Domain.Sources;
using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class AddSource
{
    public sealed record Command(SourceForCreationDto SourceToAdd) : IRequest<SourceDto>;

    public sealed class Handler : IRequestHandler<Command, SourceDto>
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

        public async Task<SourceDto> Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSources);

            var sourceToAdd = request.SourceToAdd.ToSourceForCreation();
            var source = Source.Create(sourceToAdd);

            await _sourceRepository.Add(source, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return source.ToSourceDto();
        }
    }
}