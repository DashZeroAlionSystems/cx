namespace CX.Container.Server.Domain.Citations.Dtos
{
    /// <summary>
    /// Citation for uploaded file
    /// </summary>
    public class CitationUploadDto
    {
        public Guid SourceDocumentId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IFormFile File { get; set; }
    }
}
