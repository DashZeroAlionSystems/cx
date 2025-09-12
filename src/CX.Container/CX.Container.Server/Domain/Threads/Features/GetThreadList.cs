namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Services;
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

public static class GetThreadList
{
    public sealed record Query(ThreadParametersDto QueryParameters) : IRequest<PagedList<ThreadDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<ThreadDto>>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IThreadRepository threadRepository, IHeimGuardClient heimGuard)
        {
            _threadRepository = threadRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<ThreadDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageThreads);

            var collection = _threadRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToThreadDtoQueryable();

            return await PagedList<ThreadDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}