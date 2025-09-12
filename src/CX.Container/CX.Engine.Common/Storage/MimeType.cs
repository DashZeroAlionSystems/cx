// -----------------------------------------------------------------------------
//  FileExtension → Content-Type converter
//  • Covers the most common formats (see enum below)
//  • Falls back to application/octet-stream when unknown
// -----------------------------------------------------------------------------

using JetBrains.Annotations;

public enum MimeType
{
    // ---- Text / data
    Txt,
    Csv,
    Json,
    Xml,
    Html,
    Css,
    Js,
    // ---- Images
    Jpg,
    Png,
    Gif,
    Bmp,
    Webp,
    Svg,
    Tiff,
    // ---- Audio
    Mp3,
    Ogg,
    Wav,
    Flac,
    // ---- Video
    Mp4,
    Webm,
    Avi,
    Mov,
    // ---- Docs
    Pdf,
    Doc,
    Docx,
    Xls,
    Xlsx,
    Ppt,
    Pptx,
    // ---- Archives
    Zip,
    Tar,
    Gz,
    Rar,
    SevenZ,
    // ---- Binary fallback
    Bin
}

public static class MimeTypeExtensions
{
    public static string GetExtension(this MimeType mimeType)
    {
        return mimeType switch
        {
            MimeType.Txt => "txt",
            MimeType.Csv => "csv",
            MimeType.Json => "json",
            MimeType.Xml => "xml",
            MimeType.Html => "html",
            MimeType.Css => "css",
            MimeType.Js => "js",
            MimeType.Jpg => "jpg",
            MimeType.Png => "png",
            MimeType.Gif => "gif",
            MimeType.Bmp => "bmp",
            MimeType.Webp => "webp",
            MimeType.Svg => "svg",
            MimeType.Tiff => "tiff",
            MimeType.Mp3 => "mp3",
            MimeType.Ogg => "ogg",
            MimeType.Wav => "wav",
            MimeType.Flac => "flac",
            MimeType.Mp4 => "mp4",
            MimeType.Webm => "webm",
            MimeType.Avi => "avi",
            MimeType.Mov => "mov",
            MimeType.Pdf => "pdf",
            MimeType.Doc => "doc",
            MimeType.Docx => "docx",
            MimeType.Xls => "xlsx",
            MimeType.Xlsx => "xlsx",
            MimeType.Ppt => "ppt",
            MimeType.Pptx => "pptx",
            MimeType.Zip => "zip",
            MimeType.Tar => "tar",
            MimeType.Gz => "gz",
            MimeType.Rar => "rar",
            MimeType.SevenZ => "7z",
            _ => "bin"
        };
    }
    private static readonly Dictionary<MimeType, string> _map = new()
    {
        // text / data
        [MimeType.Txt]  = "text/plain",
        [MimeType.Csv]  = "text/csv",
        [MimeType.Json] = "application/json",
        [MimeType.Xml]  = "application/xml",
        [MimeType.Html] = "text/html",
        [MimeType.Css]  = "text/css",
        [MimeType.Js]   = "application/javascript",
        // images
        [MimeType.Jpg]  = "image/jpeg",
        [MimeType.Png]  = "image/png",
        [MimeType.Gif]  = "image/gif",
        [MimeType.Bmp]  = "image/bmp",
        [MimeType.Webp] = "image/webp",
        [MimeType.Svg]  = "image/svg+xml",
        [MimeType.Tiff] = "image/tiff",
        // audio
        [MimeType.Mp3]  = "audio/mpeg",
        [MimeType.Ogg]  = "audio/ogg",
        [MimeType.Wav]  = "audio/wav",
        [MimeType.Flac] = "audio/flac",
        // video
        [MimeType.Mp4]  = "video/mp4",
        [MimeType.Webm] = "video/webm",
        [MimeType.Avi]  = "video/x-msvideo",
        [MimeType.Mov]  = "video/quicktime",
        // docs
        [MimeType.Pdf]  = "application/pdf",
        [MimeType.Doc]  = "application/msword",
        [MimeType.Docx] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [MimeType.Xls]  = "application/vnd.ms-excel",
        [MimeType.Xlsx] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [MimeType.Ppt]  = "application/vnd.ms-powerpoint",
        [MimeType.Pptx] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        // archives
        [MimeType.Zip]   = "application/zip",
        [MimeType.Tar]   = "application/x-tar",
        [MimeType.Gz]    = "application/gzip",
        [MimeType.Rar]   = "application/vnd.rar",
        [MimeType.SevenZ]= "application/x-7z-compressed",
        // fallback
        [MimeType.Bin]  = "application/octet-stream"
    };

    /// <summary>
    /// Returns the RFC-compliant MIME string (e.g. "image/png").
    /// </summary>
    public static string GetContentType(this MimeType mt) => _map[mt];

    public static MimeType GetMimeType([CanBeNull] this string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return MimeType.Bin;
        return _map.FirstOrDefault(x => x.Value == contentType).Key;
    }
    
    /// <summary>
    /// Tries to map a file extension (".png", "PDF", etc.) to a MimeType enum.
    /// Unknown extensions are mapped to MimeType.Bin.
    /// </summary>
    public static MimeType ToMimeType(this string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return MimeType.Bin;

        var ext = extension.TrimStart('.').ToLowerInvariant();

        return ext switch
        {
            "txt"  => MimeType.Txt,
            "csv"  => MimeType.Csv,
            "json" => MimeType.Json,
            "xml"  => MimeType.Xml,
            "html" => MimeType.Html,
            "htm"  => MimeType.Html,
            "css"  => MimeType.Css,
            "js"   => MimeType.Js,

            "jpg" or "jpeg" => MimeType.Jpg,
            "png"  => MimeType.Png,
            "gif"  => MimeType.Gif,
            "bmp"  => MimeType.Bmp,
            "webp" => MimeType.Webp,
            "svg"  => MimeType.Svg,
            "tif" or "tiff" => MimeType.Tiff,

            "mp3"  => MimeType.Mp3,
            "ogg"  => MimeType.Ogg,
            "wav"  => MimeType.Wav,
            "flac" => MimeType.Flac,

            "mp4"  => MimeType.Mp4,
            "webm" => MimeType.Webm,
            "avi"  => MimeType.Avi,
            "mov"  => MimeType.Mov,

            "pdf"  => MimeType.Pdf,
            "doc"  => MimeType.Doc,
            "docx" => MimeType.Docx,
            "xls"  => MimeType.Xls,
            "xlsx" => MimeType.Xlsx,
            "ppt"  => MimeType.Ppt,
            "pptx" => MimeType.Pptx,

            "zip"  => MimeType.Zip,
            "tar"  => MimeType.Tar,
            "gz"   => MimeType.Gz,
            "tgz"  => MimeType.Gz,
            "rar"  => MimeType.Rar,
            "7z"   => MimeType.SevenZ,

            _ => MimeType.Bin
        };
    }
}
