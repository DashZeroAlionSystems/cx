namespace CX.Container.Server.Extensions.Services.ConsumerRegistrations;

using MassTransit;
using RabbitMQ.Client;
using CX.Container.Server.Domain;

public static class TrainAiOnDocumentRegistration
{
    public static void TrainAiOnDocument(this IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        //cfg.ReceiveEndpoint("source-documents-training", re =>
        //{
        //    // turns off default fanout settings
        //    re.ConfigureConsumeTopology = false;

        //    // a replicated queue to provide high availability and data safety. available in RMQ 3.8+
        //    re.SetQuorumQueue();

        //    // enables a lazy queue for more stable cluster with better predictive performance.
        //    // Please note that you should disable lazy queues if you require really high performance, if the queues are always short, or if you have set a max-length policy.
        //    re.SetQueueArgument("declare", "lazy");

        //    // the consumers that are subscribed to the endpoint
        //    re.ConfigureConsumer<AiDocumentTrainer>(context);

        //    // the binding of the intermediary exchange and the primary exchange
        //    re.Bind("train-source-documents", e =>
        //    {
        //        e.RoutingKey = "";
        //        e.ExchangeType = ExchangeType.Direct;
        //    });
        //});
    }
}