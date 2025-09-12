using CX.Engine.Common.Meta;

namespace CX.Engine.DocExtractors.Text;

public interface IDocumentTextExtractor
{
    Task<string> ExtractToTextAsync(Stream stream, DocumentMeta meta);
}

public static class IDocumentTextExtractorExt
{
    public static async Task<string> ExtractToTextAsync(this IDocumentTextExtractor extractor, string pdfPath, DocumentMeta meta)
    {
        await using var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await extractor.ExtractToTextAsync(fs, meta);
    }
}