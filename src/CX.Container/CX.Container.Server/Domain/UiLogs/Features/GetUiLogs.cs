namespace CX.Container.Server.Domain.UiLogs.Features;

using CX.Container.Server.Domain.UiLogs.Dtos;
using CX.Container.Server.Domain.UiLogs.Services;
using CX.Container.Server.Exceptions;
using Mappings;
using MediatR;

public static class GetUiLogs
{
    public sealed record Query(Guid UiLogsId) : IRequest<UiLogsDto>;

    public sealed class Handler : IRequestHandler<Query, UiLogsDto>
    {
        private readonly IUiLogsRepository _uiLogsRepository;

        public Handler(IUiLogsRepository uiLogsRepository)
        {
            _uiLogsRepository = uiLogsRepository;
        }

        public async Task<UiLogsDto> Handle(Query request, CancellationToken cancellationToken)
        {
            var result = await _uiLogsRepository.GetById(request.UiLogsId, cancellationToken: cancellationToken);
            return result.ToUiLogsDto();
        }
    }
}