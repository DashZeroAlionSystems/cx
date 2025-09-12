namespace CX.Container.Server.Domain.Nodes.Dtos;

using CX.Container.Server.Resources;

public sealed class NodeParametersDto : BasePaginationParameters
{
    public Guid ProjectId { get; set; }
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
