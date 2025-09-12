namespace CX.Container.Server.Domain.Projects.Dtos;

using CX.Container.Server.Resources;

public sealed class ProjectParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
