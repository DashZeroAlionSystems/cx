using CX.Engine.Common.Tracing;

namespace CX.Engine.DocExtractors.Images;

public class DocImageExtraction
{
    public readonly PDFToJpg PDFToJpg;

    private readonly DocXToPDF _docXToPdf;

    public DocImageExtraction(DocXToPDF docXToPdf, PDFToJpg pdfToJpg)
    {
        _docXToPdf = docXToPdf ?? throw new ArgumentNullException(nameof(docXToPdf));
        PDFToJpg = pdfToJpg ?? throw new ArgumentNullException(nameof(pdfToJpg));
    }

    public async Task<List<string>> ExtractImagesAsync(Guid documentId, Stream stream) =>
        await CXTrace.Current.SpanFor(CXTrace.Section_ExtractImages, new
        {
            DocumentId = documentId.ToString()
        }).ExecuteAsync(async _ =>
        {
            var images = await PDFToJpg.GetImagesAsync(documentId);
            if (images is { Count: > 0 })
                return images;

            switch (await FileFormatChecker.GetFileFormatAsync(stream))
            {
                case FileFormat.PDF:
                    return await PDFToJpg.PDFToImagesAsync(documentId, stream);
                case FileFormat.DocX:
                    if (_docXToPdf.Options.Enabled)
                    {
                        var pdfBytes = await _docXToPdf.ConvertToPDFAsync(stream);
                        var pdfStream = new MemoryStream(pdfBytes);
                        return await PDFToJpg.PDFToImagesAsync(documentId, pdfStream);
                    }
                    else
                        return new();
                default: return new();
            }
        });
}