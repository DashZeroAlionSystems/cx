using System.Text;
using CX.Engine.Common;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;

namespace CX.Engine.DocExtractors.Text;

public class DocTextExtractionRouter
{
    public static bool FileExtensionForcesFormat = true;
    
    private readonly PDFPlumber _pdfPlumber;
    private readonly PythonDocX _pythonDocx;
    private readonly Gpt4VisionExtractor _vision;

    public DocTextExtractionRouter(PDFPlumber pdfPlumber, PythonDocX pythonDocx, Gpt4VisionExtractor vision)
    {
        _pdfPlumber = pdfPlumber ?? throw new ArgumentNullException(nameof(pdfPlumber));
        _pythonDocx = pythonDocx ?? throw new ArgumentNullException(nameof(pythonDocx));
        _vision = vision ?? throw new ArgumentNullException(nameof(vision));
    }

    private async Task<string> ExtractImagesToTextAsync(IBinaryStore imageStore, List<string> images, DocumentMeta meta)
    {
        var tasks = new Task<string>[images.Count];

        for (var i = 0; i < images.Count; i++)
        {
            async Task<string> ExtractImageAsync(int pageNo)
            {
                var image = images[pageNo];
                var imageStream = await imageStore.GetBytesAsync(image);
                if (imageStream != null)
                    return await _vision.ExtractToTextAsync(new MemoryStream(imageStream), meta);
                //else
                //    throw new InvalidOperationException($"Missing image for page {pageNo}");

                return "";
            }

            tasks[i] = ExtractImageAsync(i);
        }

        await Task.WhenAll(tasks);

        var sb = new StringBuilder();
        for (var i = 0; i < tasks.Length; i++)
        {
            var task = tasks[i];
            sb.AppendLine($"--- PAGE {i + 1} ---");
            sb.AppendLine();
            sb.AppendLine(task.Result);
        }

        return sb.ToString();
    }

    public Task<string> ExtractToTextAsync(string fileName, Stream stream, DocumentMeta meta, IBinaryStore imageStore = null, List<string> images = null,
        bool preferImageTextExtraction = true) =>
        CXTrace.Current.SpanFor("extract-to-text", new {
            FileName = fileName,
            Images = images?.Count,
            ImageStore = imageStore != null,
            preferImageTextExtraction = preferImageTextExtraction
        }).ExecuteAsync(async _ =>
        {
            var processed = false;
            string res = null;

            if (imageStore != null && images?.Count > 0 && preferImageTextExtraction)
            {
                processed = true;
                res = await ExtractImagesToTextAsync(imageStore, images, meta);
            }

            if (FileExtensionForcesFormat && !processed && fileName != null)
            {
                var ext = Path.GetExtension(fileName).ToLower();
                if (ext.StartsWith("."))
                    ext = ext[1..];

                switch (ext)
                {
                    case "pdf":
                        res = await _pdfPlumber.ExtractToTextAsync(stream, meta);
                        processed = true;
                        break;
                    case "docx":
                        res = await _pythonDocx.ExtractToTextAsync(stream, meta);
                        processed = true;
                        break;
                    case "txt":
                    case "csv":
                        res = await stream.ReadToEndAsync();
                        processed = true;
                        break;
                }
            }

            if (!processed)
                switch (await FileFormatChecker.GetFileFormatAsync(stream))
                {
                    case FileFormat.PDF:
                        res = await _pdfPlumber.ExtractToTextAsync(stream, meta);
                        processed = true;
                        break;
                    case FileFormat.DocX:
                        res = await _pythonDocx.ExtractToTextAsync(stream, meta);
                        processed = true;
                        break;
                    case FileFormat.Text:
                        res = await stream.ReadToEndAsync();
                        processed = true;
                        break;
                }

            if (!processed)
                throw new NotSupportedException("File format not supported");

            if (res == null)
                throw new InvalidOperationException("Failed to extract text from document");

            // Ensure that only valid UTF-8 characters are returned.
            return Utf8Utils.Sanitize(res);
        });
}