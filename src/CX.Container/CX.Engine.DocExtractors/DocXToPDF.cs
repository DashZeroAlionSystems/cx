using System.Diagnostics;
using CX.Engine.Common;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Options;

namespace CX.Engine.DocExtractors;

public class DocXToPDF
{
    public readonly DocXToPDFOptions Options;
    private readonly IBinaryStore _store;
    private readonly SemaphoreSlim slimLock = new(1, 1);
    
    
    public DocXToPDF(IOptions<DocXToPDFOptions> options, IServiceProvider sp)
    {
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();
        _store = sp.GetRequiredNamedService<IBinaryStore>(Options.BinaryStore);
    }
    
    
    private static async Task<byte[]> ConvertDocXToPdfAsync(Stream docxStream)
    {
        var path = Path.GetTempFileName();
        File.Delete(path);
        var dir = Path.GetDirectoryName(path);
        var inputPath = Path.ChangeExtension(path, ".docx");
        var outputPath = Path.ChangeExtension(path, ".pdf");

        try
        {
            docxStream.Position = 0;
            // Write the input Stream to the temporary DOCX file
            await using (var fileStream = new FileStream(inputPath, FileMode.Create, FileAccess.Write))
                await docxStream.CopyToAsync(fileStream);

            // Create the process to run LibreOffice in headless mode
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "libreoffice",
                Arguments = $"--headless --convert-to pdf --outdir \"{dir}\" \"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                // Capture the output and error streams
                _ = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"LibreOffice conversion failed: {error}");
            }

            // Return the generated PDF file as a Stream
            var outputStream = new MemoryStream();
            await using (var fileStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read))
                await fileStream.CopyToAsync(outputStream);

            outputStream.Position = 0; // Reset the stream position to the beginning
            return outputStream.ToArray();
        }
        finally
        {
            TryDeleteFile(inputPath);
            TryDeleteFile(outputPath);
        }
    }
    
    /// <summary>
    /// Converts a DocX file to a PDF file.
    /// </summary>
    /// <param name="stream">The DocX stream.  Will be read multiple times.</param>
    public async Task<byte[]> ConvertToPDFAsync(Stream stream) =>
        await CXTrace.Current.SpanFor(CXTrace.Section_DocXToPDF, null)
            .ExecuteAsync(async span =>
            {
                var cacheKey = await stream.GetSHA256Async();
                using var _ = await slimLock.UseAsync();
                
                var cached = await _store.GetBytesAsync(cacheKey);
                
                if (cached != null)
                {
                    span.Output = new { ContentLength = cached.Length, FromCache = true, CacheKey = cacheKey };
                    return cached;
                }

                var content = await ConvertDocXToPdfAsync(stream);
                span.Output = new { ContentLength = content.Length, FromCache = false, CacheKey = cacheKey };
                await _store.SetBytesAsync(cacheKey, content);
                return content;
            });

}