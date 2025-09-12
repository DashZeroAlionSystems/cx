namespace CX.Container.Server.Domain.Sources.Dtos;

using CX.Container.Server.Resources;

public sealed class SourceParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
