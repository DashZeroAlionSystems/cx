namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;

using CX.Container.Server.Resources;

public sealed class AgriculturalActivityTypeParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
