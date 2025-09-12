namespace CX.Container.Server.Domain.Sources.Features;

using CX.Container.Server.Domain.Sources.Services;
using CX.Container.Server.Services;
using MediatR;

public static class DeleteSource
{
    public sealed record Command(Guid SourceId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ISourceRepository _sourceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(ISourceRepository sourceRepository, IUnitOfWork unitOfWork)
        {
            _sourceRepository = sourceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var recordToDelete = await _sourceRepository.GetById(request.SourceId, cancellationToken: cancellationToken);
            _sourceRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}