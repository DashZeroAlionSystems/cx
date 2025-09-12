namespace CX.Container.Server.Domain.Countries.Mappings;

using CX.Container.Server.Domain.Countries.Dtos;
using CX.Container.Server.Domain.Countries.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class CountryMapper
{
    public static partial CountryForCreation ToCountryForCreation(this CountryForCreationDto countryForCreationDto);
    public static partial CountryForUpdate ToCountryForUpdate(this CountryForUpdateDto countryForUpdateDto);
    public static partial CountryDto ToCountryDto(this Country country);
    public static partial IQueryable<CountryDto> ToCountryDtoQueryable(this IQueryable<Country> country);
}