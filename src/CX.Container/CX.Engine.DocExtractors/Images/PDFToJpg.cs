using CX.Engine.Common;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using Microsoft.Extensions.Options;

namespace CX.Engine.DocExtractors.Images;

public class PDFToJpg
{
    public static string GetPageId(string sDocumentId, int pageNo) => $"{sDocumentId} #{pageNo}";
    
    public readonly IBinaryStore ImageStore;

    private readonly PDFToJpgOptions _options;
    private readonly PythonProcess _python;
    private readonly IJsonStore _pageCountStore;
    private readonly KeyedSemaphoreSlim _keyedLock = new();

    public PDFToJpg(IOptions<PDFToJpgOptions> options, IServiceProvider sp)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _python = sp.GetRequiredNamedService<PythonProcess>(_options.PythonProcess);
        _pageCountStore = sp.GetRequiredNamedService<IJsonStore>(_options.JsonDocumentStore);
        ImageStore = sp.GetRequiredNamedService<IBinaryStore>(_options.BinaryImageStore);
    }

    private async Task Delete_InternalAsync(Guid documentId)
    {
        var sDocumentId = documentId.ToString();

        var count = await _pageCountStore.GetAsync<int>(sDocumentId);

        if (count == 0)
            return;

        var lst = new List<Task>();
        for (var i = 1; i <= count; i++)
            lst.Add(ImageStore.DeleteAsync(GetPageId(sDocumentId, i)));
        await Task.WhenAll(lst);

        await _pageCountStore.DeleteAsync(sDocumentId);
    }

    public async Task DeleteAsync(Guid documentId)
    {
        using var _ = await _keyedLock.UseAsync(documentId.ToString());
        await Delete_InternalAsync(documentId);
    }

    /// <summary>
    /// Not safe when called in parallel with <see cref="PDFToImagesAsync"/>.
    /// </summary>
    public async Task ClearAsync()
    {
        await ImageStore.ClearAsync();
        await _pageCountStore.ClearAsync();
    }

    public async Task<List<string>> GetImagesAsync(Guid documentId)
    {
        var sDocumentId = documentId.ToString();
        List<string> images = new();

        using var _ = await _keyedLock.UseAsync(sDocumentId);

        var count = await _pageCountStore.GetAsync<int>(sDocumentId);

        if (count == 0)
            return images;

        for (var i = 1; i <= count; i++)
            images.Add(GetPageId(sDocumentId, i));

        return images;
    }

    public Task<List<string>> PDFToImagesAsync(Guid documentId, Stream stream) =>
        CXTrace.Current.SpanFor(CXTrace.Section_PDFToImages, new
        {
            DocumentId = documentId
        }).ExecuteAsync(async span =>
        {
            var sDocumentId = documentId.ToString();
            List<string> images = [];

            using var _ = await _keyedLock.UseAsync(sDocumentId);

            await Delete_InternalAsync(documentId);

            var first = true;
            var pageCount = 0;
            await _python.StreamToFilesAsync(_options.ScriptPath,
                stream,
                async (file, pageNo, count) =>
                {
                    if (first)
                    {
                        await _pageCountStore.SetAsync(sDocumentId, count);
                        pageCount = count;
                        first = false;
                    }

                    var pageId = GetPageId(sDocumentId, pageNo);
                    var bytes = await File.ReadAllBytesAsync(file);
                    await ImageStore.SetBytesAsync(pageId, bytes);
                },
                _options.PopplerPath);

            for (var i = 1; i <= pageCount; i++)
                images.Add(GetPageId(sDocumentId, i));

            return images;
        });
}