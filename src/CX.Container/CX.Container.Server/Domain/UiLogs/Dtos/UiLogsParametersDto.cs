namespace CX.Container.Server.Domain.UiLogs.Dtos;

using CX.Container.Server.Resources;

public sealed class UiLogsParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
