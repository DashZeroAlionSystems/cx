namespace CX.Container.Server.Domain.AgriculturalActivities.Mappings;

using Dtos;
using Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class AgriculturalActivityMapper
{
    public static partial AgriculturalActivityForCreation ToAgriculturalActivityForCreation(this AgriculturalActivityForCreationDto agriculturalActivityForCreationDto);
    public static partial AgriculturalActivityForUpdate ToAgriculturalActivityForUpdate(this AgriculturalActivityForUpdateDto agriculturalActivityForUpdateDto);
    public static partial AgriculturalActivityDto ToAgriculturalActivityDto(this AgriculturalActivity agriculturalActivity);
    public static partial IQueryable<AgriculturalActivityDto> ToAgriculturalActivityDtoQueryable(this IQueryable<AgriculturalActivity> agriculturalActivity);
}