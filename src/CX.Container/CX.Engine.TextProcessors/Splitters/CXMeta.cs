using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Meta;
using JetBrains.Annotations;

namespace CX.Engine.TextProcessors.Splitters;

/// <summary>
/// Metadata for chunks and segments.
/// </summary>
public class CXMeta : Dictionary<string, object>, ISerializeJson
{
    public const string Key_PageNos = "Page Numbers";
    public const string Key_ContainsTables = "Contains Table(s)";
    public const string Key_Attachments = "Attachments";
    public const string Key_DocumentId = "Document Id";
    public const string Key_DocumentDescription = "Source Document Description";
    public const string Key_Tags = "Source Document Tags";
    public const string Key_SourceDocument = "Source Document";
    public const string Key_Info = "Info";
    public const string Key_SourceDocumentGroup = "Source Document Group";
    public const string Key_SandboxUrl = "Sandbox URL";

    [ContractAnnotation("createIfNotExists:true => notnull")]
    public HashSet<int> GetPageNos(bool createIfNotExists)
    {
        var res = (HashSet<int>)this.GetValueOrDefault(Key_PageNos);

        if (res == null && createIfNotExists)
            this[Key_PageNos] = res = new();

        return res;
    }

    public List<AttachmentInfo> GetAttachments(bool createIfNotExists)
    {
        var res = (List<AttachmentInfo>)this.GetValueOrDefault(Key_Attachments);

        if (res == null && createIfNotExists)
            this[Key_Attachments] = res = new();

        return res;
    }

    public bool ContainsTables
    {
        get => this.GetValueOrDefault(Key_ContainsTables) as bool? ?? false;
        set
        {
            if (!value)
                Remove(Key_ContainsTables);
            else
                this[Key_ContainsTables] = true;
        }
    }

    public Guid? DocumentId
    {
        get => this.GetValueOrDefault(Key_DocumentId) as Guid?;
        set
        {
            if (value == null)
                Remove(Key_DocumentId);
            else
                this[Key_DocumentId] = value;
        }
    }

    public string Tags
    {
        get => this.GetValueOrDefault(Key_Tags) as string;
        set
        {
            if (value == null)
                Remove(Key_Tags);
            else
                this[Key_Tags] = value;
        }
    }

    public string SourceDocumentGroup
    {
        get => this.GetValueOrDefault(Key_SourceDocumentGroup) as string;
        set
        {
            if (value == null)
                Remove(Key_SourceDocumentGroup);
            else
                this[Key_SourceDocumentGroup] = value;
        }
    }

    public string SourceDocument
    {
        get => this.GetValueOrDefault(Key_SourceDocument) as string;
        set
        {
            if (value == null)
                Remove(Key_SourceDocument);
            else
                this[Key_SourceDocument] = value;
        }
    }

    public JsonDocument Info
    {
        get => this.GetValueOrDefault(Key_Info) as JsonDocument;
        set
        {
            if (value == null)
                Remove(Key_Info);
            else
                this[Key_Info] = value;
        }
    }

    public string DocumentDescription
    {
        get => this.GetValueOrDefault(Key_DocumentDescription) as string;
        set
        {
            if (value == null)
                Remove(Key_DocumentDescription);
            else
                this[Key_DocumentDescription] = value;
        }
    }

    public void AssignFrom(CXMeta src)
    {
        Clear();
        
        var sPageNos = src.GetPageNos(false); 
        if (sPageNos != null)
            this[Key_PageNos] = sPageNos.ShallowClone();
        
        var sAttachments = src.GetAttachments(false);
        if (sAttachments != null)
        {
            var lst = new List<AttachmentInfo>();
            foreach (var att in sAttachments)
                lst.Add(att.Clone());
            this[Key_Attachments] = lst;
        }

        foreach (var kvp in src)
            if (!ContainsKey(kvp.Key))
                this[kvp.Key] = kvp.Value;
    }

    public void MergeMeta(DocumentMeta src)
    {
        var sAttachments = src.Attachments;
        var dAttachments = GetAttachments(sAttachments != null);
        var sDocumentId = src.Id;
        var dDocumentId = DocumentId;

        if (sDocumentId != null && dDocumentId != null && sDocumentId != dDocumentId)
            throw new InvalidOperationException(
                $"Cannot merge metadata with different document ids: {sDocumentId} vs {dDocumentId}");

        if (dAttachments != null)
        {
            this[Key_Attachments] = dAttachments;

            if (sAttachments != null && sAttachments != dAttachments)
                foreach (var att in sAttachments)
                    if (!dAttachments.Contains(att))
                        dAttachments.AddRange(sAttachments);
        }

        if (src.Organization != null)
            this["Organization"] = src.Organization;

        if (src.ColumnHeaders != null)
            this["Column Headers"] = src.ColumnHeaders;

        if (src.Id != null)
            DocumentId = src.Id;

        if (src.SourceDocument != null)
            this[Key_SourceDocument] = src.SourceDocument;

        if (src.SourceDocumentGroup != null)
            this[Key_SourceDocumentGroup] = src.SourceDocumentGroup;

        if (src.SandboxUrl != null)
            this[Key_SandboxUrl] = src.SandboxUrl;

        Info = src.Info;
    }

    public void MergeMeta(CXMeta src)
    {
        var sPageNos = src.GetPageNos(false);
        var dPageNos = GetPageNos(sPageNos != null);
        var sAttachments = src.GetAttachments(false);
        var dAttachments = GetAttachments(sAttachments != null);
        var sDocumentId = src.DocumentId;
        var dDocumentId = DocumentId;
        var sGroupid = src.SourceDocumentGroup;
        var dGroupId = SourceDocumentGroup;

        if (sDocumentId != null && dDocumentId != null && sDocumentId != dDocumentId)
            throw new InvalidOperationException(
                $"Cannot merge metadata with different document ids: {sDocumentId} vs {dDocumentId}");
        
        if (sGroupid != null && dGroupId != null && sGroupid != dGroupId)
            throw new InvalidOperationException(
                $"Cannot merge metadata with different document group ids: {sGroupid} vs {dGroupId}");

        foreach (var kvp in src)
            this[kvp.Key] = kvp.Value;

        if (dPageNos != null)
        {
            this[Key_PageNos] = dPageNos;

            if (sPageNos != null)
                dPageNos.AddRange(sPageNos);
        }

        if (dAttachments != null)
        {
            this[Key_Attachments] = dAttachments;

            if (sAttachments != null)
                foreach (var att in sAttachments)
                    if (!dAttachments.Any(s => s.IsSameAttachment(att)))
                        dAttachments.Add(att);
        }
    }

    public void Serialize(Utf8JsonWriter jw)
    {
        jw.WriteStartObject();
        foreach (var kvp in this)
        {
            if (kvp.Value == null)
                continue;

            jw.WritePropertyName(kvp.Key);
            switch (kvp.Value)
            {
                case HashSet<int> hi:
                    jw.WriteStartArray();
                    foreach (var i in hi)
                        jw.WriteNumberValue(i);
                    jw.WriteEndArray();
                    break;
                case HashSet<string> hs:
                    jw.WriteStartArray();
                    foreach (var s in hs)
                        jw.WriteStringValue(s);
                    jw.WriteEndArray();
                    break;
                case List<AttachmentInfo> atts:
                    jw.WriteStartArray();
                    foreach (var att in atts)
                        att.Serialize(jw);
                    jw.WriteEndArray();
                    break;
                case JsonDocument jdoc:
                    jdoc.WriteTo(jw);
                    break;
                case Guid g:
                    jw.WriteStringValue(g.ToString());
                    break;
                case bool b:
                    jw.WriteBooleanValue(b);
                    break;
                case string s:
                    jw.WriteStringValue(s);
                    break;
                case int i:
                    jw.WriteNumberValue(i);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unexpected value type {kvp.Value?.GetType().Name} for key {kvp.Key}");
            }
        }

        jw.WriteEndObject();
    }
}