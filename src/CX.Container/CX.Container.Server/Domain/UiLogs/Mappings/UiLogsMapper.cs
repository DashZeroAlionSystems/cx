namespace CX.Container.Server.Domain.UiLogs.Mappings;

using CX.Container.Server.Domain.UiLogs.Dtos;
using CX.Container.Server.Domain.UiLogs.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class UiLogsMapper
{
    public static partial UiLogsForCreation ToUiLogsForCreation(this UiLogsForCreationDto uiLogsForCreationDto);
    public static partial UiLogsForUpdate ToUiLogsForUpdate(this UiLogsForUpdateDto uiLogsForUpdateDto);
    public static partial UiLogsDto ToUiLogsDto(this UiLogs uiLogs);
    public static partial IQueryable<UiLogsDto> ToUiLogsDtoQueryable(this IQueryable<UiLogs> uiLogs);
}