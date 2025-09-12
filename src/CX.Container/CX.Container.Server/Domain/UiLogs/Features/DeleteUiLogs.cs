namespace CX.Container.Server.Domain.UiLogs.Features;

using CX.Container.Server.Domain.UiLogs.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using MediatR;

public static class DeleteUiLogs
{
    public sealed record Command(Guid UiLogsId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IUiLogsRepository _uiLogsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IUiLogsRepository uiLogsRepository, IUnitOfWork unitOfWork)
        {
            _uiLogsRepository = uiLogsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var recordToDelete = await _uiLogsRepository.GetById(request.UiLogsId, cancellationToken: cancellationToken);
            _uiLogsRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}