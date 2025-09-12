using System.Text.Json;
using System.Text.Json.Serialization;
using CX.Engine.Common.Json;
using JetBrains.Annotations;

namespace CX.Engine.Common;

/// <summary>
/// This class represents a file attachment, with properties for identifying the file,
/// accessing its contents, and providing additional metadata. It implements the ISerializeJson interface 
/// which means it can be serialized to JSON format.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AttachmentInfo : ISerializeJson
{
    //NB: Properties are cloned by the <see cref="Clone"/> method.
    
    
    /// <summary>
    /// Not persisted.
    /// </summary>
    [JsonInclude] public Guid? CitationId;
    
    [JsonInclude] public string FileName;
    [JsonInclude] public string FileUrl;
    [JsonInclude] public string Description;
    [JsonInclude] public string Context;

    public Func<Task<Stream>> DoGetContentStreamAsync;

    public AttachmentInfo()
    {
    }

    public AttachmentInfo(BinaryReader br)
    {
        Deserialize(br);
    }
    
    public bool IsSameAttachment(AttachmentInfo other) =>
        other != null &&
        FileName == other.FileName &&
        FileUrl == other.FileUrl &&
        Description == other.Description &&
        Context == other.Context;

    /// <summary>
    /// This method serializes the current instance into a JSON object using the given Utf8JsonWriter.
    /// </summary>
    /// <param name="jw">The Utf8JsonWriter to use for writing the JSON object.</param>
    /// <remarks>
    /// The resulting JSON object will have properties corresponding to the public data members of the current instance.
    /// The names of these properties will be as follows: fileName, fileId, fileUrl, description, and context.
    /// The values will be the values of the respective data members. 
    /// </remarks>
    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        if (!string.IsNullOrWhiteSpace(FileName))
        {
            jw.WritePropertyName("fileName");
            jw.WriteStringValue(FileName);
        }

        if (!string.IsNullOrWhiteSpace(FileUrl))
        {
            jw.WritePropertyName("fileUrl");
            jw.WriteStringValue(FileUrl);
        }

        if (!string.IsNullOrWhiteSpace(Description))
        {
            jw.WritePropertyName("description");
            jw.WriteStringValue(Description);
        }

        if (!string.IsNullOrWhiteSpace(Context))
        {
            jw.WritePropertyName("context");
            jw.WriteStringValue(Context);
        }

        jw.WriteEndObject();
    }

    public string FullUrl => (FileUrl?.StartsWith("/") ?? false) ? "sandbox:/" + FileUrl : FileUrl;

    public string AsMarkdownLink() => $"[{FileName}]({FullUrl})";

    public void Serialize(BinaryWriter bw)
    {
        bw.WriteNullable(FileName);
        bw.WriteNullable((string)null);//Was FileId
        bw.WriteNullable(FileUrl);
        bw.WriteNullable(Description);
        bw.WriteNullable(Context);
    }

    /// <summary>
    /// This method deserializes a JSON object into an instance of AttachmentInfo.
    /// </summary>
    /// <param name="br">The BinaryReader used to read the JSON object.</param>
    /// <remarks>
    /// The method reads the property values from the JSON object and assigns them to the corresponding data members of the AttachmentInfo instance.
    /// The expected properties in the JSON object are fileName, fileId, fileUrl, description, and context.
    /// If any of these properties are missing or their values are null, the corresponding data member in the AttachmentInfo instance will be null.
    /// </remarks>
    public void Deserialize(BinaryReader br)
    {
        FileName = br.ReadStringNullable();
        _ = br.ReadStringNullable();//Was FileId
        FileUrl = br.ReadStringNullable();
        Description = br.ReadStringNullable();
        Context = br.ReadStringNullable();
    }
    
    public AttachmentInfo Clone() =>
        new()
        {
            CitationId = CitationId,
            FileName = FileName,
            FileUrl = FileUrl,
            Description = Description,
            Context = Context,
            DoGetContentStreamAsync = DoGetContentStreamAsync
        };

    public override string ToString() => FileName + "(" + FileUrl + ")";
}