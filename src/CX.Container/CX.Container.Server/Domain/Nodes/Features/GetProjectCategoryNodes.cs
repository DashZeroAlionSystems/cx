namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using MediatR;

public static class GetProjectCategoryNodes
{
    public sealed record Query(Guid ProjectId) : IRequest<List<CategoryNodeDto>>;

    public sealed class Handler : IRequestHandler<Query, List<CategoryNodeDto>>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _heimGuard = heimGuard;
        }
        public async Task<List<CategoryNodeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            return await _nodeRepository.GetCategoryNodesByProjectId(request.ProjectId, cancellationToken: cancellationToken);                        
        }
    }
}