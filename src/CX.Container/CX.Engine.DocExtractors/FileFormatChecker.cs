using System.IO.Compression;
using CX.Engine.Common.Tracing;

namespace CX.Engine.DocExtractors;

public static class FileFormatChecker
{
    public static async Task<bool> IsTextStreamAsync(Stream stream)
    {
        const int sampleSize = 8096; // Read first 8096 bytes (8 KB)
        var suspectedBinary = 0;
        
        if (stream == null || stream.Length == 0)
            return true; // Null or empty streams are considered text files

        var buffer = new byte[sampleSize];
        stream.Position = 0;
        var bytesRead = await stream.ReadAsync(buffer, 0, sampleSize);
        stream.Position = 0;

        for (var i = 0; i < bytesRead; i++)
        {
            var b = buffer[i];
            if (b is > 0x7F and < 0xA0) // Check for non-text bytes
                suspectedBinary++;
        }

        if (suspectedBinary / (double)bytesRead > 0.01)
            return false;

        return true; // If no non-text bytes were found, it's a text file
    }
    
    public static Task<FileFormat> GetFileFormatAsync(Stream stream) =>
        CXTrace.Current.SpanFor(CXTrace.Section_DetectFileFormat, null).ExecuteAsync(async _ =>
        {

            var buffer = new byte[8];
            stream.Position = 0;
            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
            stream.Position = 0;

            //Very short file, too short for most formats
            if (read < 8)
            {
                if (await IsTextStreamAsync(stream))
                    return FileFormat.Text;

                return FileFormat.Other;
            }


            // PDF files start with "%PDF-" (Hex: 25 50 44 46 2D)
            if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46 && buffer[4] == 0x2D)
            {
                return FileFormat.PDF;
            }

            // DOCX files (Office Open XML) start with "PK" (Hex: 50 4B) which indicates a ZIP file
            if (buffer[0] == 0x50 && buffer[1] == 0x4B)
            {
                if (IsDocxFile(stream))
                    return FileFormat.DocX;
            }

            if (await IsTextStreamAsync(stream))
                return FileFormat.Text;

            return FileFormat.Other;
        });

    private static bool IsDocxFile(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            var hasContentTypes = false;
            var hasWordFolder = false;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                {
                    hasContentTypes = true;
                }

                if (entry.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase))
                {
                    hasWordFolder = true;
                }

                if (hasContentTypes && hasWordFolder)
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't read the archive, it's not a valid DOCX file
        }

        return false;
    }
}
