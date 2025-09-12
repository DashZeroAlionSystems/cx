namespace CX.Container.Server.Extensions.Services.ProducerRegistrations;

using MassTransit;
using SharedKernel.Messages;
using RabbitMQ.Client;

public static class QueueDocumentForTrainingRegistration
{
    public static void QueueDocumentForTraining(this IRabbitMqBusFactoryConfigurator cfg)
    {
        //cfg.Message<ISourceDocumentMessage>(e => e.SetEntityName("train-source-documents")); // name of the primary exchange
        //cfg.Publish<ISourceDocumentMessage>(e => e.ExchangeType = ExchangeType.Direct); // primary exchange type

        //// configuration for the exchange and routing key
        //cfg.Send<ISourceDocumentMessage>(e =>
        //{
        //    // **Use the `UseRoutingKeyFormatter` to configure what to use for the routing key when sending a message of type `ISourceDocumentMessage`**
        //    /* Examples
        //    *
        //    * Direct example: uses the `ProductType` message property as a key
        //    * e.UseRoutingKeyFormatter(context => context.Message.ProductType.ToString());
        //    *
        //    * Topic example: uses the VIP Status and ClientType message properties to make a key.
        //    * e.UseRoutingKeyFormatter(context =>
        //    * {
        //    *     var vipStatus = context.Message.IsVip ? "vip" : "normal";
        //    *     return $"{vipStatus}.{context.Message.ClientType}";
        //    * });
        //    */
        //});
    }
}