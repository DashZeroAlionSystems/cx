namespace CX.Container.Server.Domain.Nodes.Mappings;

using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Nodes.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class NodeMapper
{
    public static partial NodeForCreation ToNodeForCreation(this NodeForCreationDto nodeForCreationDto);
    public static partial NodeForUpdate ToNodeForUpdate(this NodeForUpdateDto nodeForUpdateDto);
    public static partial NodeDto ToNodeDto(this Node node);
    public static partial IQueryable<NodeDto> ToNodeDtoQueryable(this IQueryable<Node> node);
}