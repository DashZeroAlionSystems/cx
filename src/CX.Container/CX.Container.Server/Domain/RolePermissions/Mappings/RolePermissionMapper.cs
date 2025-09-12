namespace CX.Container.Server.Domain.RolePermissions.Mappings;

using CX.Container.Server.Domain.RolePermissions.Dtos;
using CX.Container.Server.Domain.RolePermissions.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class RolePermissionMapper
{
    public static partial RolePermissionForCreation ToRolePermissionForCreation(this RolePermissionForCreationDto rolePermissionForCreationDto);
    public static partial RolePermissionForUpdate ToRolePermissionForUpdate(this RolePermissionForUpdateDto rolePermissionForUpdateDto);
    public static partial RolePermissionDto ToRolePermissionDto(this RolePermission rolePermission);
    public static partial IQueryable<RolePermissionDto> ToRolePermissionDtoQueryable(this IQueryable<RolePermission> rolePermission);
}