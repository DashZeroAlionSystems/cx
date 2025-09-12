namespace CX.Container.Server.Domain.Preferences.Mappings;

using CX.Container.Server.Domain.Preferences.Dtos;
using CX.Container.Server.Domain.Preferences.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class PreferenceMapper
{
    public static partial PreferenceForCreation ToPreferenceForCreation(this PreferenceForCreationDto preferenceForCreationDto);
    public static partial PreferenceForUpdate ToPreferenceForUpdate(this PreferenceForUpdateDto preferenceForUpdateDto);
    public static partial PreferenceDto ToPreferenceDto(this Preference preference);
    public static partial IQueryable<PreferenceDto> ToPreferenceDtoQueryable(this IQueryable<Preference> preference);
}