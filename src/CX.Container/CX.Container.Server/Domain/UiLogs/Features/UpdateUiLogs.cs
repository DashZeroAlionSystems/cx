namespace CX.Container.Server.Domain.UiLogs.Features;

using CX.Container.Server.Domain.UiLogs;
using CX.Container.Server.Domain.UiLogs.Dtos;
using CX.Container.Server.Domain.UiLogs.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Domain.UiLogs.Models;
using CX.Container.Server.Exceptions;
using Mappings;
using MediatR;

public static class UpdateUiLogs
{
    public sealed record Command(Guid UiLogsId, UiLogsForUpdateDto UpdatedUiLogsData) : IRequest;

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
            var uiLogsToUpdate = await _uiLogsRepository.GetById(request.UiLogsId, cancellationToken: cancellationToken);
            var uiLogsToAdd = request.UpdatedUiLogsData.ToUiLogsForUpdate();
            uiLogsToUpdate.Update(uiLogsToAdd);

            _uiLogsRepository.Update(uiLogsToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}