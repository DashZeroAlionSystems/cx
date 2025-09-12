namespace CX.Container.Server.Domain;

using SharedKernel.Messages;
using MassTransit;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CX.Container.Server.Databases;
using MassTransit.Transports;

public static class NewSourceDocumentWasAdded
{
    public sealed record NewSourceDocumentWasAddedCommand() : IRequest<bool>;

    public sealed class Handler : IRequestHandler<NewSourceDocumentWasAddedCommand, bool>
    {
        //private readonly IPublishEndpoint _publishEndpoint;
        private readonly AelaDbContext _db;

        //public Handler(AelaDbContext db, IPublishEndpoint publishEndpoint)
        //{
        //    _publishEndpoint = publishEndpoint;
        //    _db = db;
        //}
        public Handler(AelaDbContext db)
        {
            //_publishEndpoint = publishEndpoint;
            _db = db;
        }

        public async Task<bool> Handle(NewSourceDocumentWasAddedCommand request, CancellationToken cancellationToken)
        {
            var message = new SourceDocumentMessage
            {
                // map content to message here or with mapperly
            };
            //await _publishEndpoint.Publish<ISourceDocumentMessage>(message, cancellationToken);
            await Task.CompletedTask;
            return true;
        }
    }
}