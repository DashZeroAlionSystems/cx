namespace CX.Engine.Common.Storage;

public class StorageResponseBase
{
    public Stream Content { get; set; }
    public MimeType ContentType { get; set; }
}