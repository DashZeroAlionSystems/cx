using System.Text.Json.Serialization;

namespace CX.Container.Server.Domain.MessageCitations.Dtos
{
    /// <summary>
    /// Citations for the response message
    /// </summary>
    public class MessageCitationDto
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("ocr_text")]
        public string OcrText { get; set; }

        [JsonPropertyName("decorator_text")]
        public string DecoratorText { get; set; }

        [JsonPropertyName("import_warnings")]
        public string ImportWarnings { get; set; }
    }
}
