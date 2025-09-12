using System.Text.Json;
using System.Text.Json.Serialization;

namespace CX.Engine.Common.Meta;

public class DocumentMeta
{
    [JsonInclude] [JsonPropertyName("id")]
    public Guid? Id;
    
    [JsonInclude] [JsonPropertyName("pages")]
    public int? Pages;

    [JsonInclude] [JsonPropertyName("containsTables")]
    public bool? ContainsTables;

    [JsonInclude] [JsonPropertyName("attachments")]
    public List<AttachmentInfo> Attachments;

    [JsonInclude] [JsonPropertyName("description")]
    public string Description;

    [JsonInclude] [JsonPropertyName("sourceDocument")]
    public string SourceDocument;

    [JsonInclude] [JsonPropertyName("sourceDocumentGroup")]
    public string SourceDocumentGroup;

    [JsonInclude] [JsonPropertyName("sandboxUrl")]
    public string SandboxUrl;

    [JsonInclude] [JsonPropertyName("organization")]
    public string Organization;

    [JsonInclude] [JsonPropertyName("columnHeaders")]
    public string ColumnHeaders;

    public List<string> ExtractionErrors = null;

    [JsonInclude] [JsonPropertyName("tags")]
    public HashSet<string> Tags;

    [JsonInclude] [JsonPropertyName("info")]
    public JsonDocument Info;

    public void AddAttachments(IEnumerable<AttachmentInfo> att) => (Attachments ??= []).AddRange(att);

    public DocumentMeta DeepClone()
    {
        var res = new DocumentMeta();
        res.Id = Id;
        res.Description = Description;
        res.SourceDocument = SourceDocument;
        res.Pages = Pages;
        res.ContainsTables = ContainsTables;
        res.Organization = Organization;
        res.ColumnHeaders = ColumnHeaders;
        res.Info = Info;
        res.Tags = Tags.ShallowClone();

        if (Attachments != null)
        {
            res.Attachments = new(Attachments.Count);
            foreach (var att in Attachments)
                res.Attachments.Add(att.Clone());
        }

        return res;
    }
}