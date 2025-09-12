namespace CX.Container.Server.Domain.Countries.Dtos;

using CX.Container.Server.Resources;

public sealed class CountryParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
