using System.Collections.Concurrent;
using CX.Engine.Common;
using CX.Engine.DocExtractors.Images;
using Microsoft.Extensions.Options;

namespace CX.Engine.FileServices;

public class FileService
{
    private readonly PDFToJpg _pdfToJpg;
    private readonly FileServiceOptions _options;
    private readonly ConcurrentDictionary<string, CXFileInfo> _fileMetas = new();

    public FileService(IOptions<FileServiceOptions> options, PDFToJpg pdfToJpg)
    {
        _pdfToJpg = pdfToJpg ?? throw new ArgumentNullException(nameof(pdfToJpg));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    public async Task AddFileMetaAsync(string fileId, CXFileInfo cxFileInfo)
    {
        cxFileInfo.SHA256 ??= await (await GetStreamAsync(cxFileInfo)).GetSHA256Async();

        if (!_fileMetas.TryAdd(fileId, cxFileInfo))
            throw new InvalidOperationException("File info already uploaded for file id: " + fileId);
    }

    public async Task<CXFileInfo> BuildFromStreamAsync(MemoryStream stream)
    {
        var cxFileInfo = new CXFileInfo();
        cxFileInfo.SHA256 ??= await stream.GetSHA256Async();
        cxFileInfo.FileId = cxFileInfo.SHA256;

        var res = GetFileMetaBySHA256(cxFileInfo.SHA256, false);
        if (res != null)
            return res;

        cxFileInfo.Path = Path.Combine(_options.FileCacheDirectory, cxFileInfo.SHA256 + ".bin");

        if (!File.Exists(cxFileInfo.Path))
        {
            await using var fs = new FileStream(cxFileInfo.Path, FileMode.Create);
            stream.Position = 0;
            await stream.CopyToAsync(fs);
        }

        await AddFileMetaAsync(cxFileInfo.FileId, cxFileInfo);
        return cxFileInfo;
    }

    public CXFileInfo GetFileMetaBySHA256(string sha256, bool throwIfNotExists)
    {
        foreach (var meta in _fileMetas)
            if (string.Equals(meta.Value.SHA256, sha256, StringComparison.InvariantCultureIgnoreCase))
                return meta.Value;

        if (throwIfNotExists)
            throw new InvalidOperationException($"File info not found for sha256: {sha256}");

        return null;
    }

    public CXFileInfo GetFileMeta(string fileId, bool throwIfNotExists)
    {
        if (!_fileMetas.TryGetValue(fileId, out var cxFileInfo) && throwIfNotExists)
            throw new InvalidOperationException($"File info not found for file id: {fileId}");

        return cxFileInfo;
    }


    public Task<Stream> GetStreamAsync(CXFileInfo cxFileInfo)
    {
        if (!File.Exists(cxFileInfo.Path))
            throw new InvalidOperationException($"Invalid file path for {cxFileInfo.FileId}: {cxFileInfo.Path}");

        return Task.FromResult((Stream)new FileStream(cxFileInfo.Path, FileMode.Open, FileAccess.Read));
    }


    public Task<Stream> GetStreamFromFileIdAsync(string fileId)
    {
        if (!_fileMetas.TryGetValue(fileId, out var cxFileInfo))
            throw new InvalidOperationException($"File info not found for file id: {fileId}");

        return GetStreamAsync(cxFileInfo);
    }

    public Task<Stream> GetContentStreamAsync(AttachmentInfo info)
    {
        var getStream = info.DoGetContentStreamAsync;

        if (info.FileUrl?.StartsWith("/api/page-images/") ?? false)
        {
            var parts = info.FileUrl.Split("/");
            //0 = empty root
            //1 = api
            //2 = page-images
            //3 = document id
            //4 = page number

            if (parts.Length == 5)
            {
                var sDocId = parts[3];
                var sPageNo = parts[4];
                
                if (Guid.TryParse(sDocId, out _) && int.TryParse(sPageNo, out var pageNo))
                    getStream ??= () => _pdfToJpg.ImageStore.GetStreamAsync(PDFToJpg.GetPageId(sDocId, pageNo));
            }
        }

        //We no longer support file service in this way
        //if (info.FileId != null)
        //    getStream ??= () => GetStreamFromFileIdAsync(info.FileId)!;

        if (getStream == null)
            return Task.FromResult<Stream>(null);

        return getStream();
    }
}