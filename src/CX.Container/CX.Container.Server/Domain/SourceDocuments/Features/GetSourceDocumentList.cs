namespace CX.Container.Server.Domain.SourceDocuments.Features;

using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Wrappers;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Resources;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;
using QueryKit;
using QueryKit.Configuration;

public static class GetSourceDocumentList
{
    public sealed record Query(SourceDocumentParametersDto QueryParameters) : IRequest<PagedList<SourceDocumentDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<SourceDocumentDto>>
    {
        private readonly ISourceDocumentRepository _sourceDocumentRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(ISourceDocumentRepository sourceDocumentRepository, IHeimGuardClient heimGuard)
        {
            _sourceDocumentRepository = sourceDocumentRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<SourceDocumentDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageSourceDocuments);

            var collection = _sourceDocumentRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration(config =>
            {
                config.Property<SourceDocument>(p => p.Status.Value).HasQueryName("Status");
                config.Property<SourceDocument>(p => p.DocumentSourceType.Value).HasQueryName("DocumentSourceType");
            });
            
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToSourceDocumentDtoQueryable();

            return await PagedList<SourceDocumentDto>.CreateAsync(
                dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}