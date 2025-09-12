using System.Text.Json.Serialization;

namespace CX.Container.Server.Domain.Citations.Dtos
{
    /// <summary>
    /// Dto to be sent to AI
    /// </summary>
    public class CitationUrlDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
