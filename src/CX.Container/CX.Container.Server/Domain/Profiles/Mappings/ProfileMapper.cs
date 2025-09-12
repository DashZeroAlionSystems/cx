namespace CX.Container.Server.Domain.Profiles.Mappings;

using CX.Container.Server.Domain.Profiles.Dtos;
using CX.Container.Server.Domain.Profiles.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class ProfileMapper
{
    public static partial ProfileForCreation ToProfileForCreation(this ProfileForCreationDto profileForCreationDto);
    public static partial ProfileForUpdate ToProfileForUpdate(this ProfileForUpdateDto profileForUpdateDto);
    public static partial ProfileDto ToProfileDto(this Profile profile);
    public static partial IQueryable<ProfileDto> ToProfileDtoQueryable(this IQueryable<Profile> profile);
}