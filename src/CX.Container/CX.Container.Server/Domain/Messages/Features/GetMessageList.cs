namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Services;
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

public static class GetMessageList
{
    public sealed record Query(MessageParametersDto QueryParameters) : IRequest<PagedList<MessageDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedList<MessageDto>>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IHeimGuardClient _heimGuard;

        public Handler(IMessageRepository messageRepository, IHeimGuardClient heimGuard)
        {
            _messageRepository = messageRepository;
            _heimGuard = heimGuard;
        }

        public async Task<PagedList<MessageDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);

            var collection = _messageRepository.Query().AsNoTracking();

            var queryKitConfig = new CustomQueryKitConfiguration();
            var queryKitData = new QueryKitData()
            {
                Filters = request.QueryParameters.Filters,
                SortOrder = request.QueryParameters.SortOrder,
                Configuration = queryKitConfig
            };
            var appliedCollection = collection.ApplyQueryKit(queryKitData);
            var dtoCollection = appliedCollection.ToMessageDtoQueryable();

            return await PagedList<MessageDto>.CreateAsync(dtoCollection,
                request.QueryParameters.PageNumber,
                request.QueryParameters.PageSize,
                cancellationToken);
        }
    }
}