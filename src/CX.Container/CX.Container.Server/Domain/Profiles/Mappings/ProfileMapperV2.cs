namespace CX.Container.Server.Domain.Profiles.Mappings;

using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Domain.Profiles.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class ProfileMapperV2
{
    public static partial ProfileForCreationV2 ToProfileForCreation(this ProfileForCreationDtoV2 profileForCreationDto);
    public static partial ProfileForUpdateV2 ToProfileForUpdate(this ProfileForUpdateDtoV2 profileForUpdateDto);
    public static partial ProfileDtoV2 ToProfileDtoV2(this Profile profile);
    public static partial IQueryable<ProfileDtoV2> ToProfileDtoQueryableV2(this IQueryable<Profile> profile);
}