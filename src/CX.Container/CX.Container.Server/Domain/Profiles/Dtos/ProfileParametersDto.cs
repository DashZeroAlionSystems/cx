namespace CX.Container.Server.Domain.Profiles.Dtos;

using CX.Container.Server.Resources;

public sealed class ProfileParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
