namespace CX.Container.Server.Domain;

using MassTransit;
using SharedKernel.Messages;
using System.Threading.Tasks;
using Databases;

public sealed class AiDocumentTrainer : IConsumer<ISourceDocumentMessage>
{
    private readonly AelaDbContext _db;

    public AiDocumentTrainer(AelaDbContext db)
    {
        _db = db;
    }

    public Task Consume(ConsumeContext<ISourceDocumentMessage> context)
    {
        // do work here

        return Task.CompletedTask;
    }
}