namespace CX.Container.Server.Domain.SourceDocuments.Dtos;

using CX.Container.Server.Resources;

public sealed class SourceDocumentParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
