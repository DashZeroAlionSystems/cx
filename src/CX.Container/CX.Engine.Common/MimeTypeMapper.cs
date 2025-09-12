namespace CX.Engine.Common;

public class MimeTypeMapper
{
    public static readonly MimeTypeMapper Default = new();
    
    public readonly Dictionary<string, string> MimeTypes = new(StringComparer.InvariantCultureIgnoreCase)
    {
        { ".txt", "text/plain" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".css", "text/css" },
        { ".js", "application/javascript" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".svg", "image/svg+xml" },
        { ".pdf", "application/pdf" },
        { ".zip", "application/zip" },
        { ".tar", "application/x-tar" },
        { ".mp4", "video/mp4" },
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        // Add more types as needed
    };

    public string GetContentType(string filePath)
    {
        var extension = (filePath?.Contains('.') ?? false) ? Path.GetExtension(filePath) : null;

        if (string.IsNullOrEmpty(extension))
        {
            return "application/octet-stream"; // default binary type
        }

        if (MimeTypes.TryGetValue(extension.ToLower(), out var contentType))
        {
            return contentType;
        }
        
        return "application/octet-stream"; // default for unknown types
    }
}
