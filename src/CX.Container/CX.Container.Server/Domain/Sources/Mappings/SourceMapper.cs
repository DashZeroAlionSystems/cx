namespace CX.Container.Server.Domain.Sources.Mappings;

using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Domain.Sources.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class SourceMapper
{
    public static partial SourceForCreation ToSourceForCreation(this SourceForCreationDto sourceForCreationDto);
    public static partial SourceForUpdate ToSourceForUpdate(this SourceForUpdateDto sourceForUpdateDto);
    public static partial SourceDto ToSourceDto(this Source source);
    public static partial IQueryable<SourceDto> ToSourceDtoQueryable(this IQueryable<Source> source);
}