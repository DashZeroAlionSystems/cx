namespace SharedKernel.Messages
{
    using System;

    public interface ISourceDocumentMessage
    {
        public Guid SourceDocumentId { get; set; }
    }

    public class SourceDocumentMessage : ISourceDocumentMessage
    {
        public Guid SourceDocumentId { get; set; }
    }
}