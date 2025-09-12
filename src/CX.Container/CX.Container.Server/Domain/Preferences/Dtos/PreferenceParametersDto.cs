namespace CX.Container.Server.Domain.Preferences.Dtos;

using CX.Container.Server.Resources;

public sealed class PreferenceParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
