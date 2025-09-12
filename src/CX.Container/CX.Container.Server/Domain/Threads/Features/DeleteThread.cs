namespace CX.Container.Server.Domain.Threads.Features;

using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Threads.DomainEvents;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

public static class DeleteThread
{
    public sealed record Command(Guid ThreadId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IThreadRepository _threadRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<Handler> _logger;

        public Handler(IThreadRepository threadRepository, IUnitOfWork unitOfWork, ILogger<Handler> logger)
        {
            _threadRepository = threadRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _unitOfWork.WrapInTransactionAsync(async (db, ct) =>
            {
                await DeleteThread(db, ct);
            }, cancellationToken);


            // local method
            async Task DeleteThread(AelaDbContext db, CancellationToken ct)
            {
                try
                {
                    db.QueueDomainEvent(new ThreadDeleted { Id = request.ThreadId });

                    await db.Messages.Where(m => m.ThreadId == request.ThreadId).ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDeleted, true));
                    await db.Threads.Where(t => t.Id == request.ThreadId).ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDeleted, true));

                    _logger.LogInformation("Deleted thread {ThreadId}", request.ThreadId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Delete of thread {ThreadId} failed", request.ThreadId);
                    throw;
                }
            }
        }
    }
}