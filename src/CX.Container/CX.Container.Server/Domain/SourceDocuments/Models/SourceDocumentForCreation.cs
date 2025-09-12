namespace CX.Container.Server.Domain.SourceDocuments.Models;
public sealed class SourceDocumentForCreation
{
    public Guid? SourceId { get; set; }
    public Guid? NodeId { get; set; }
    public string DisplayName { get; set; }
    public string Name { get; set; }
    public string Tags { get; set; }
    public string Language { get; set; }
    public string Description { get; set; }
    public string DocumentSourceType { get; set; }
    public string Url { get; set; }
}
