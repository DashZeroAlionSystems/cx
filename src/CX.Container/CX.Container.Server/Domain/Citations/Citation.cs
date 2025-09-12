using CX.Container.Server.Domain.Citations.DomainEvents;
using CX.Container.Server.Domain.Citations.Dtos;
using CX.Container.Server.Domain.SourceDocuments;

namespace CX.Container.Server.Domain.Citations;

public class Citation : Entity<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Guid SourceDocumentId { get; private set; }
    public byte[] Content { get; set; }
    public string FileType { get; set; }

    public string Url { get; set; }

    public SourceDocument SourceDocument { get; private set; }

    protected Citation()
    {
    } // For EF + Mocking

    public static Citation Create(Guid sourceDocumentId, CitationUploadDto citationUploadDto)
    {
        var newCitation = new Citation();

        newCitation.Id = Guid.NewGuid();
        newCitation.Description = citationUploadDto.Description;
        newCitation.Name = citationUploadDto.Name;
        newCitation.Content = ConvertFormFileToByteArray(citationUploadDto.File);
        newCitation.SourceDocumentId = sourceDocumentId;
        newCitation.Url = $"/api/citations/{newCitation.Id}";
        newCitation.FileType = citationUploadDto.File.ContentType;

        newCitation.QueueDomainEvent(new CitationCreated() { Citation = newCitation });

        return newCitation;
    }

    public Citation Update(CitationForUpdateDto citationForUpdateDto)
    {
        if (citationForUpdateDto.Name != null)
        {
            Name = citationForUpdateDto.Name;
        }
        if (citationForUpdateDto.Description != null)
        {
            Description = citationForUpdateDto.Description;
        }

        QueueDomainEvent(new CitationUpdated() { Id = Id });
        return this;
    }

    private static byte[] ConvertFormFileToByteArray(IFormFile file)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            file.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}