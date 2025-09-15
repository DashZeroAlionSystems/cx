namespace CX.Container.Server.Domain.Projects.Mappings;

using CX.Container.Server.Domain.Projects.Dtos;
using CX.Container.Server.Domain.Projects.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class ProjectMapper
{
	public static partial ProjectForCreation ToProjectForCreation(this ProjectForCreationDto projectForCreationDto);
	public static partial ProjectForUpdate ToProjectForUpdate(this ProjectForUpdateDto projectForUpdateDto);
	public static partial ProjectDto ToProjectDto(this Project project);
	public static partial IQueryable<ProjectDto> ToProjectDtoQueryable(this IQueryable<Project> project);
}