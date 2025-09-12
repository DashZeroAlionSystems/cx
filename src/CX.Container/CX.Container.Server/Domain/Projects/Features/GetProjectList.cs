namespace CX.Container.Server.Domain.Projects.Features;

using CX.Container.Server.Domain.Projects.Dtos;
using CX.Container.Server.Domain.Projects.Services;
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

public static class GetProjectList
{
    public sealed record Query(ProjectParametersDto QueryParameters) : IRequest<PagedList<ProjectDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<ProjectDto>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IProjectRepository projectRepository, IHeimGuardClient heimGuard)
        {
            _projectRepository = projectRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<ProjectDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageProjects);

            var collection = _projectRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToProjectDtoQueryable();

            return await PagedList<ProjectDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}