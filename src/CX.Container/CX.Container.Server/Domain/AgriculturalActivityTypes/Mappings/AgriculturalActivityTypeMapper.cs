namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Mappings;

using CX.Container.Server.Domain.AgriculturalActivityTypes.Dtos;
using CX.Container.Server.Domain.AgriculturalActivityTypes.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class AgriculturalActivityTypeMapper
{
    public static partial AgriculturalActivityTypeForCreation ToAgriculturalActivityTypeForCreation(this AgriculturalActivityTypeForCreationDto agriculturalActivityTypeForCreationDto);
    public static partial AgriculturalActivityTypeForUpdate ToAgriculturalActivityTypeForUpdate(this AgriculturalActivityTypeForUpdateDto agriculturalActivityTypeForUpdateDto);
    public static partial AgriculturalActivityTypeDto ToAgriculturalActivityTypeDto(this AgriculturalActivityType agriculturalActivityType);
    public static partial IQueryable<AgriculturalActivityTypeDto> ToAgriculturalActivityTypeDtoQueryable(this IQueryable<AgriculturalActivityType> agriculturalActivityType);
}