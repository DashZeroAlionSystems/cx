using CX.Container.Server.Domain.Messages;

namespace CX.Container.Server.Domain.MessageCitations;

public class MessageCitation : Entity<Guid>
{
    public string Url { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }

    public string OcrText { get; set; }

    public string DecoratorText { get; set; }

    public string ImportWarnings { get; set; }

    public Guid MessageId { get; private set; }

    public Message Message { get; private set; }

    public MessageCitation()
    {
    } // For EF + Mocking
}