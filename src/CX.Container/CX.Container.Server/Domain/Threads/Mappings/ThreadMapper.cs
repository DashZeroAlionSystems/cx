namespace CX.Container.Server.Domain.Threads.Mappings;

using CX.Container.Server.Domain.Threads.Dtos;
using CX.Container.Server.Domain.Threads.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class ThreadMapper
{
    public static partial ThreadForCreation ToThreadForCreation(this ThreadForCreationDto threadForCreationDto);
    public static partial ThreadForUpdate ToThreadForUpdate(this ThreadForUpdateDto threadForUpdateDto);
    public static partial ThreadDto ToThreadDto(this Thread thread);
    public static partial IQueryable<ThreadDto> ToThreadDtoQueryable(this IQueryable<Thread> thread);
}