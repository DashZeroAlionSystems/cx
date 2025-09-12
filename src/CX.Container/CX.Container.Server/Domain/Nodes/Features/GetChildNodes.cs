namespace CX.Container.Server.Domain.Nodes.Features;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using Microsoft.EntityFrameworkCore;
using MediatR;

public static class GetChildNodes
{
    public sealed record Query(Guid ParentId) : IRequest<List<NodeDto>>;

    public sealed class Handler : IRequestHandler<Query, List<NodeDto>>
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(INodeRepository nodeRepository, IHeimGuardClient heimGuard)
        {
            _nodeRepository = nodeRepository;
            _heimGuard = heimGuard;
        }
        public async Task<List<NodeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageNodes);

            return _nodeRepository.Query()
                .AsNoTracking()
                .Where(node => node.ParentId == request.ParentId)
                .ToNodeDtoQueryable()
                .ToList();
        }
    }
}