namespace CX.Container.Server.Domain.Users.Dtos;

using CX.Container.Server.Resources;

public sealed class UserParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
