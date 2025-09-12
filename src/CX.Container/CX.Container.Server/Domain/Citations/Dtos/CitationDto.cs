namespace CX.Container.Server.Domain.Citations.Dtos
{
    /// <summary>
    /// Dto returned to Api
    /// </summary>
    public class CitationDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] Content { get; set; }
        public string FileType { get; set; }
    }
}
