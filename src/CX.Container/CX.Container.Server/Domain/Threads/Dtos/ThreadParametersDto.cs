namespace CX.Container.Server.Domain.Threads.Dtos;

using CX.Container.Server.Resources;

public sealed class ThreadParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
