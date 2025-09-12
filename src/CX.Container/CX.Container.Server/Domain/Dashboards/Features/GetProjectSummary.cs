namespace CX.Container.Server.Domain.Dashboards.Features;

using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Domain.Dashboards.Dtos;
using MediatR;

public static class GetProjectSummary
{
    public sealed record Query(Guid ProjectId) : IRequest<ProjectSummaryDto>;

    public sealed class Handler : IRequestHandler<Query, ProjectSummaryDto>
    {
        private readonly INodeRepository _nodeRepository;
        
        public Handler(INodeRepository nodeRepository)
        {
            _nodeRepository = nodeRepository;            
        }

        public async Task<ProjectSummaryDto> Handle(Query request, CancellationToken cancellationToken)
        {
            var projectSummaryDto = await _nodeRepository.GetProjectSummaryByProjectIdAsync(request.ProjectId, cancellationToken);
            return projectSummaryDto;
        }
    }
}