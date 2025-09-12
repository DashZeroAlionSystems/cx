namespace CX.Container.Server.Domain.SourceDocuments.Mappings;

using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class SourceDocumentMapper
{
    public static partial SourceDocumentForCreation ToSourceDocumentForCreation(this SourceDocumentForCreationDto sourceDocumentForCreationDto);
    public static partial SourceDocumentForUpdate ToSourceDocumentForUpdate(this SourceDocumentForUpdateDto sourceDocumentForUpdateDto);
    public static partial SourceDocumentDto ToSourceDocumentDto(this SourceDocument sourceDocument);
    public static partial IQueryable<SourceDocumentDto> ToSourceDocumentDtoQueryable(this IQueryable<SourceDocument> sourceDocument);
    
    
    // [MapProperty(nameof(SourceDocument.Id), nameof(SourceDocumentMessage.SourceDocumentId))]
    // public static partial SourceDocumentMessage ToSourceDocumentMessage(this SourceDocument sourceDocument);
}