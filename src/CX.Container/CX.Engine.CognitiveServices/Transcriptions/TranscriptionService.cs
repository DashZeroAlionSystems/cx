using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CX.Engine.CognitiveServices.Transcriptions;
using CX.Engine.CognitiveServices.VoiceTranscripts;
using CX.Engine.Common;
using CX.Engine.Common.Tracing;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.CognitiveServices.Blobs;

public class TranscriptionService : IDisposable
{
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly IDisposable _optionsMonitorDisposable;
    private readonly SemaphoreSlim _snapshotSemaphore = new(1, 1);
    private readonly TaskCompletionSource tcsInitialized = new();
    public Task Initialized => tcsInitialized.Task;
    public SnapshotClass Snapshot;

    public class SnapshotClass
    {
        public TranscriptionServiceOptions Options;
        public BlobServiceClient BlobServiceClient;
        public BlobContainerClient BlobContainerClient;
    }

    public TranscriptionService([NotNull] string name, IOptionsMonitor<TranscriptionServiceOptions> monitor, ILogger logger, IServiceProvider sp)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsMonitorDisposable = monitor.Snapshot(() => Snapshot?.Options, SetSnapshot, _logger, sp);
    }

    private async void SetSnapshot(TranscriptionServiceOptions opts)
    {
        using var _ = await _snapshotSemaphore.UseAsync();
        try
        {
            var ss = new SnapshotClass();
            ss.Options = opts;

            ss.BlobServiceClient = new(opts.BlobConnectionString);
            ss.BlobContainerClient = ss.BlobServiceClient.GetBlobContainerClient(ss.Options.BlobContainerName);

            await ss.BlobContainerClient.CreateIfNotExistsAsync();

            Snapshot = ss;
            tcsInitialized.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set snapshot, new options will be ignored.");
        }
    }

    public Task<(string Name, string Url)> UploadFileToBlobAsync(SnapshotClass ss, Stream stream, string fileName) =>
        CXTrace.Current.SpanFor("upload-to-blob", new { Filename = fileName }).ExecuteAsync(async span =>
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            if (!fileName.FilenameHasExtension(ss.Options.SupportedFileExtensions))
                throw new TranscriptionInvalidFileFormatException(
                    $"Invalid file format.  Supported formats are: {string.Join(", ", ss.Options.SupportedFileExtensions)}");

            while (true)
            {
                var blobName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(fileName)}";
                var blobClient = ss.BlobContainerClient.GetBlobClient(blobName);

                try
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders
                    {
                        ContentType = Path.GetExtension(fileName).ToLower() switch
                        {
                            ".mp3" => "audio/mpeg",
                            ".wav" => "audio/wav",
                            ".ogg" => "audio/ogg",
                            _ => "application/octet-stream"
                        }
                    });
                }
                catch (RequestFailedException ex) when (ex.Status == 409)
                {
                    continue;
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = ss.Options.BlobContainerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(24)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var storageSharedKeyCredential = new StorageSharedKeyCredential(ss.Options.StorageAccountName, ss.Options.StorageAccountKey);
                var sasToken = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential);

                var blobUrl = $"{blobClient.Uri}?{sasToken}";
                span.Output = new { BlobName = blobName, BlobUrl = blobUrl };
                return (blobName, blobUrl);
            }
        });

    public Task DeleteBlobIfExistsAsync(SnapshotClass ss, string blobName) =>
        CXTrace.Current.SpanFor("delete-blob-if-exists", new { BlobName = blobName }).ExecuteAsync(async _ =>
        {
            var client = ss.BlobContainerClient.GetBlobClient(blobName);
            await client.DeleteIfExistsAsync();
        });

    public Task<string> CreateTranscriptionAsync(SnapshotClass ss, string audioUrl) =>
        CXTrace.Current.SpanFor("create-transcription", new { AudioUrl = audioUrl }).ExecuteAsync(async span =>
        {
            var config = ss.Options.TranscriptionConfig.ToJsonNode();
            config["contentUrls"] = new JsonArray([JsonValue.Create(audioUrl)]);
            config["displayName"] = $"transcription_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            var response = await $"{ss.Options.Endpoint}"
                .WithHeader("Ocp-Apim-Subscription-Key", ss.Options.ApiKey)
                .PostAsync(new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json"));

            var content = await response.ResponseMessage.Content.ReadAsStringAsync();

            if (!response.ResponseMessage.IsSuccessStatusCode)
                throw new InvalidOperationException($"Failed to create transcription. Status: {response.StatusCode}, Error: {content}");

            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (!result.TryGetProperty("self", out var selfProperty))
                throw new InvalidOperationException("Failed to get 'self' property from response");

            var selfUrl = selfProperty.GetString();
            if (string.IsNullOrEmpty(selfUrl))
                throw new InvalidOperationException("'self' URL is null or empty");

            var transcriptionId = selfUrl.Split('/')[^1];
            if (string.IsNullOrEmpty(transcriptionId))
                throw new InvalidOperationException("Failed to extract transcription ID from 'self' URL");

            span.Output = new { transcriptionId = transcriptionId };
            return transcriptionId;
        });

    public Transcription ConvertToTranscriptionResult(JsonElement transcription)
    {
        var phrases = transcription.GetProperty("recognizedPhrases").EnumerateArray();
        var res = new Transcription();

        foreach (var phrase in phrases.OrderBy(je => je.GetProperty("offsetMilliseconds").GetInt64()))
        {
            var speaker = phrase.GetProperty("channel").GetInt32();
            var line = phrase.GetProperty("nBest")[0].GetProperty("display").GetString();
            var offset = MiscHelpers.ParseIso8601Timespan(phrase.GetProperty("offset").GetString());
            var duration = MiscHelpers.ParseIso8601Timespan(phrase.GetProperty("duration").GetString());
            res.Phrases.Add(new(speaker, line, offset, duration));
        }

        return res;
    }

    public Task DeleteTranscriptionAsync(SnapshotClass ss, string transcriptionId) => CXTrace.Current
        .SpanFor("delete-transcription", new { Transcriptionid = transcriptionId }).ExecuteAsync(
            async _ =>
            {
                await $"{ss.Options.Endpoint}/{transcriptionId}"
                    .WithHeader("Ocp-Apim-Subscription-Key", ss.Options.ApiKey)
                    .DeleteAsync();
            });

    public Task<JsonElement> GetTranscriptionAsync(SnapshotClass ss, string transcriptionId) =>
        CXTrace.Current.SpanFor("get-transcription", new { TranscriptionId = transcriptionId }).ExecuteAsync(async span =>
        {
            var responseContent = await (await $"{ss.Options.Endpoint}/{transcriptionId}/files"
                .WithHeader("Ocp-Apim-Subscription-Key", ss.Options.ApiKey)
                .GetAsync()).GetStringAsync();

            var files = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Get transcription file URL
            var transcriptionFile = files.GetProperty("values").EnumerateArray()
                .FirstOrDefault(x => x.GetProperty("kind").GetString()?.ToLower() == "transcription");

            if (transcriptionFile.ValueKind == JsonValueKind.Undefined)
                throw new InvalidOperationException("Transcription file not found");

            var transcriptionUrl = transcriptionFile.GetProperty("links").GetProperty("contentUrl").GetString()
                                   ?? throw new InvalidOperationException("Failed to get transcription URL");

            var transcription = JsonSerializer.Deserialize<JsonElement>(await transcriptionUrl.GetStringAsync());
            span.Output = new { Transcription = transcription };
            return transcription;
        });

    private async Task<(string ErrorCode, string ErrorMessage)> GetTranscriptionAsyncError(SnapshotClass ss, string transcriptionId)
    {
        var result = JsonSerializer.Deserialize<JsonElement>(await (await $"{ss.Options.Endpoint}/{transcriptionId}"
            .WithHeader("Ocp-Apim-Subscription-Key", ss.Options.ApiKey)
            .GetAsync()).GetStringAsync());

        if (result.TryGetProperty("properties", out var properties))
        {
            if (properties.TryGetProperty("error", out var error))
            {
                var code = error.GetProperty("code").GetString() ?? "Unknown";
                var message = error.GetProperty("message").GetString() ?? "No error message";
                return (code, message);
            }
        }

        return ("N/A", "No detailed error information available");
    }

    private async Task<string> GetTranscriptionAsyncStatus(SnapshotClass ss, string transcriptionId) =>
        JsonSerializer.Deserialize<JsonElement>(await (await $"{ss.Options.Endpoint}/{transcriptionId}"
            .WithHeader("Ocp-Apim-Subscription-Key", ss.Options.ApiKey)
            .GetAsync()).GetStringAsync()).GetProperty("status").GetString() ?? throw new InvalidOperationException("Failed to get transcription status");

    public Task WaitForTranscriptionAsync(SnapshotClass ss, string transcriptionId) =>
        CXTrace.Current.SpanFor("wait-for-transcription", new { TranscriptionId = transcriptionId }).ExecuteAsync(async span =>
        {
            var maxWaits = ss.Options.MaxWaits;
            var waitCount = 0;

            while (waitCount < maxWaits)
            {
                var status = await GetTranscriptionAsyncStatus(ss, transcriptionId);

                if (status == "Succeeded")
                {
                    span.Output = new { Success = true };
                    break;
                }

                if (status == "Failed")
                {
                    var errorDetails = await GetTranscriptionAsyncError(ss, transcriptionId);
                    throw new TranscriptionApiException(errorDetails.ErrorCode, errorDetails.ErrorMessage);
                }

                await Task.Delay(ss.Options.WaitInterval);
                waitCount++;
            }

            if (waitCount >= maxWaits)
                throw new InvalidOperationException("Transcription timed out after maximum waits");
        });

    public async Task<Transcription> ProcessAsync(Stream stream, string fileName)
    {
        await Initialized;

        var ss = Snapshot;
        var blob = await UploadFileToBlobAsync(ss, stream, fileName);
        string transcriptionId = null;
        try
        {
            transcriptionId = await CreateTranscriptionAsync(ss,
                blob.Url);
            await WaitForTranscriptionAsync(ss, transcriptionId);
            var res = await GetTranscriptionAsync(ss, transcriptionId);
            var text = ConvertToTranscriptionResult(res);
            return text;
        }
        finally
        {
            if (!ss.Options.KeepBlobs)
                try
                {
                    await DeleteBlobIfExistsAsync(ss, blob.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Trying to clean up uploaded blob");
                }

            if (transcriptionId != null && !ss.Options.KeepTranscripts)
                try
                {
                    await DeleteTranscriptionAsync(ss, transcriptionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Trying to clean up transcription");
                }
        }
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}