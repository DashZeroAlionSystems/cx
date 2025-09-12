namespace CX.Container.Server.Domain.AgriculturalActivities.Dtos;

using CX.Container.Server.Resources;

public sealed class AgriculturalActivityParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
