namespace CX.Container.Server.Domain.UiLogs.Features;

using CX.Container.Server.Domain.UiLogs.Services;
using CX.Container.Server.Domain.UiLogs;
using CX.Container.Server.Domain.UiLogs.Dtos;
using CX.Container.Server.Domain.UiLogs.Models;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using Mappings;
using MediatR;

public static class AddUiLogs
{
    public sealed record Command(UiLogsForCreationDto UiLogsToAdd) : IRequest<UiLogsDto>;

    public sealed class Handler : IRequestHandler<Command, UiLogsDto>
    {
        private readonly IUiLogsRepository _uiLogsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IUiLogsRepository uiLogsRepository, IUnitOfWork unitOfWork)
        {
            _uiLogsRepository = uiLogsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<UiLogsDto> Handle(Command request, CancellationToken cancellationToken)
        {
            var uiLogsToAdd = request.UiLogsToAdd.ToUiLogsForCreation();
            var uiLogs = UiLogs.Create(uiLogsToAdd);

            await _uiLogsRepository.Add(uiLogs, cancellationToken);
            await _unitOfWork.CommitChanges(cancellationToken);

            return uiLogs.ToUiLogsDto();
        }
    }
}