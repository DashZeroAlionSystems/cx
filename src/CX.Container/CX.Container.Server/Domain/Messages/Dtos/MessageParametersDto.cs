namespace CX.Container.Server.Domain.Messages.Dtos;

using CX.Container.Server.Resources;

public sealed class MessageParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
