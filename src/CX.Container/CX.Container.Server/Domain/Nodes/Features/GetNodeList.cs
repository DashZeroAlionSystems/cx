namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Wrappers;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Resources;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;
using QueryKit;
using QueryKit.Configuration;

public static class GetNodeList
{
    public sealed record Query(NodeParametersDto QueryParameters) : IRequest<PagedList<NodeDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<NodeDto>>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<NodeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            var collection = _nodeRepository.Query().AsNoTracking();

            collection = collection.Where(node => node.ProjectId == request.QueryParameters.ProjectId);

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToNodeDtoQueryable();

            return await PagedList<NodeDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}