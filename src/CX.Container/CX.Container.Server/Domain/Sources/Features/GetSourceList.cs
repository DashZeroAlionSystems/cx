namespace CX.Container.Server.Domain.Sources.Features;

using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Domain.Sources.Services;
using CX.Container.Server.Wrappers;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Resources;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;
using QueryKit;
using QueryKit.Configuration;

public static class GetSourceList
{
    public sealed record Query(SourceParametersDto QueryParameters) : IRequest<PagedList<SourceDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<SourceDto>>
    {
        private readonly ISourceRepository _sourceRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceRepository sourceRepository, IHeimGuardClient heimGuard)
        {
            _sourceRepository = sourceRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<SourceDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSources);

            var collection = _sourceRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToSourceDtoQueryable();

            return await PagedList<SourceDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}