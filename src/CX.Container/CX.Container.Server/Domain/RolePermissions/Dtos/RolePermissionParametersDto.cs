namespace CX.Container.Server.Domain.RolePermissions.Dtos;

using CX.Container.Server.Resources;

public sealed class RolePermissionParametersDto : BasePaginationParameters
{
    public string? Filters { get; set; }
    public string? SortOrder { get; set; }
}
