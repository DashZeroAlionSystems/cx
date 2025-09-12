namespace CX.Container.Server.Domain.SourceDocuments.Models;
public sealed class SourceDocumentForUpdate
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
    public string Status { get; set; }
    public string OCRTaskID { get; set; }
    public string ErrorText { get; set; }
    public string ImportWarnings { get; set; }
    public string OCRText { get; set; }
    public string DecoratorText { get; set; }
    public string DecoratorTaskID { get; set; }
    public string TrainingTaskID { get; set; }
    public DateTime DateTrained { get; set; }
}
