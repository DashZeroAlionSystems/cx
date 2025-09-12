namespace CX.Container.Server.Domain.UiLogs.Features;

using CX.Container.Server.Domain.UiLogs.Dtos;
using CX.Container.Server.Domain.UiLogs.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Resources;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;
using QueryKit;
using QueryKit.Configuration;
using CX.Container.Server.Wrappers;

public static class GetUiLogsList
{
    public sealed record Query(UiLogsParametersDto QueryParameters) : IRequest<PagedList<UiLogsDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<UiLogsDto>>
    {
        private readonly IUiLogsRepository _uiLogsRepository;

        public Handler(IUiLogsRepository uiLogsRepository)
        {
            _uiLogsRepository = uiLogsRepository;
        }

        public async Task<PagedList<UiLogsDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var collection = _uiLogsRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToUiLogsDtoQueryable();

            return await PagedList<UiLogsDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}