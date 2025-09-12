using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Meta;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Stores.Binary;
using Microsoft.Extensions.Options;

namespace CX.Engine.DocExtractors.Text;

public class Gpt4VisionExtractor : IDocumentTextExtractor
{
    private readonly OpenAIChatAgent _chatAgent;
    private readonly Gpt4VisionExtractorOptions _options;
    private readonly IJsonStore _jsonStore;
    private readonly KeyedSemaphoreSlim _keyedLock = new();

    public Gpt4VisionExtractor(IOptions<Gpt4VisionExtractorOptions> options, IServiceProvider sp)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _chatAgent = (OpenAIChatAgent)sp.GetRequiredNamedService<IChatAgent>(_options.ChatAgent);
        _jsonStore = sp.GetRequiredNamedService<IJsonStore>(_options.JsonStore);
    }

    public async Task<string> ExtractToTextAsync(Stream stream, DocumentMeta meta)
    {
        var sha = await stream.GetSHA256Async();
        using var _ = await _keyedLock.UseAsync(sha);

        if (_options.UseCache)
        {
            var cached = await _jsonStore.GetAsync<string>(sha);

            if (cached != null)
                return cached;
        }

        var req = _chatAgent.GetRequest(_options.Question, systemPrompt: _options.SystemPrompt);
        await req.AttachImageAsync(stream);

        var res = await _chatAgent.RequestAsync(req);
        var answer = res.Answer ?? "";
        
        if (_options.UseCache)
            await _jsonStore.SetAsync(sha, answer);
        
        return answer;
    }

    public async Task<string> ExtractToTextAsync(
        string fileName,
        Stream documentStream,
        DocumentMeta meta,
        IBinaryStore imageStore,
        List<string> images,
        bool preferImageTextExtraction = true)
    {
        if (!images.Any())
            return await ExtractToTextAsync(documentStream, meta);

        var results = new List<string>();

        foreach (var imageId in images)
        {
            using var imageStream = await imageStore.GetStreamAsync(imageId);
            if (imageStream == null) continue;

            var pageText = await ExtractToTextAsync(imageStream, meta);
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                results.Add(pageText);
            }
        }

        return string.Join("\n\n", results);
    }
}