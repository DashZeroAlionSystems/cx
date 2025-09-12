using System.Text.Json;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using JetBrains.Annotations;

namespace CX.Engine.CognitiveServices.VoiceTranscripts;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TranscriptionServiceOptions : IValidatable
{
    public string StorageAccountName { get; set; }
    public string StorageAccountKey { get; set; }
    public string BlobConnectionString { get; set; }
    public string BlobContainerName { get; set; }
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string[] SupportedFileExtensions { get; set; } = [".wav", ".mp3", ".ogg", ".m4a"];
    public int MaxWaits { get; set; }
    public TimeSpan WaitInterval { get; set; }
    public bool KeepTranscripts { get; set; }
    public bool KeepBlobs { get; set; }

    [UseJsonDocumentSetup] public JsonDocument TranscriptionConfig;

    public void Validate()
    {
        if (TranscriptionConfig == null)
            throw new ArgumentException($"{nameof(TranscriptionConfig)} is required");

        if (string.IsNullOrWhiteSpace(BlobContainerName))
            throw new InvalidOperationException($"{nameof(BlobContainerName)} is required");
        
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException($"{nameof(Endpoint)} is required");
        
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException($"{nameof(ApiKey)} is required");
        
        if (string.IsNullOrWhiteSpace(BlobConnectionString))
            throw new InvalidOperationException($"{nameof(BlobConnectionString)} is required");
        
        if (MaxWaits < 1)
            throw new InvalidOperationException($"{nameof(MaxWaits)} must be greater than 0");
        
        if (WaitInterval < TimeSpan.FromMilliseconds(1))
            throw new InvalidOperationException($"{nameof(WaitInterval)} must be greater than 1 millisecond");
        
        if (string.IsNullOrWhiteSpace(StorageAccountName))
            throw new InvalidOperationException($"{nameof(StorageAccountName)} is required");
        
        if (string.IsNullOrWhiteSpace(StorageAccountKey))
            throw new InvalidOperationException($"{nameof(StorageAccountKey)} is required");
        
        if (SupportedFileExtensions == null || SupportedFileExtensions.Length == 0)
            throw new InvalidOperationException($"{nameof(SupportedFileExtensions)} is required");
        
        foreach (var extension in SupportedFileExtensions)
            if (!string.IsNullOrWhiteSpace(extension) && !extension.StartsWith(".")) 
                throw new InvalidOperationException($"{nameof(SupportedFileExtensions)} must start with a period (.)");
    }
}