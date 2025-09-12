using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using CX.Container.Server.Configurations;
using Microsoft.Extensions.Options;
namespace CX.Container.Server.Services;

public sealed class MicrosoftCognitiveSpeechService : IMicrosoftCognitiveSpeechService
{
    private readonly CognitiveSpeechOptions _options;
    private readonly ILogger<MicrosoftCognitiveSpeechService> _logger;

    public MicrosoftCognitiveSpeechService(IOptions<CognitiveSpeechOptions> options, ILogger<MicrosoftCognitiveSpeechService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }


    public async Task<string> RecognizeSpeechFromFileAsync(string audioFilePath, string language)
    {
        try
        {
            _logger.LogInformation("Starting speech recognition from file: {AudioFilePath} with language: {Language}", audioFilePath, language);

            var speechConfig = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
            speechConfig.SpeechRecognitionLanguage = language;

            var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);

            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
            var result = await speechRecognizer.RecognizeOnceAsync();

            switch (result.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    _logger.LogInformation("Recognized text: {Text}", result.Text);
                    return result.Text;

                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    _logger.LogError("Speech recognition canceled. Reason: {Reason}, ErrorDetails: {ErrorDetails}", cancellation.Reason, cancellation.ErrorDetails);
                    throw new Exception($"Speech recognition canceled. Reason: {cancellation.Reason}, ErrorDetails: {cancellation.ErrorDetails}");

                case ResultReason.NoMatch:
                    _logger.LogWarning("No speech could be recognized from the audio file: {AudioFilePath}", audioFilePath);
                    throw new Exception("No speech could be recognized from the audio.");

                default:
                    _logger.LogError("Speech recognition failed for file: {AudioFilePath}", audioFilePath);
                    throw new Exception("Speech recognition failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during speech recognition from file: {AudioFilePath}", audioFilePath);
            throw;
        }
    }
        
    public async Task<string> RecognizeSpeechFromByteArrayAsync(byte[] audioBytes, string language)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                language = _options.Language;
            }

            _logger.LogInformation("Starting speech recognition from byte array with language: {Language}", language);

            var speechConfig = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
            speechConfig.SpeechRecognitionLanguage = language;

            var transcription = new List<(int Index, string Text)>();
            var tasks = new List<Task>();

            int segmentLength = 120000; // 120 seconds
            int totalLength = audioBytes.Length;
            int offset = 0;
            int segmentIndex = 0;

            while (offset < totalLength)
            {
                var length = Math.Min(segmentLength, totalLength - offset);
                var segment = new byte[length];
                Array.Copy(audioBytes, offset, segment, 0, length);

                int currentSegmentIndex = segmentIndex;
                tasks.Add(Task.Run(async () =>
                {
                    using (var audioInputStream = AudioInputStream.CreatePushStream())
                    {
                        var audioConfig = AudioConfig.FromStreamInput(audioInputStream);

                        _ = Task.Run(() =>
                        {
                            using (var memoryStream = new MemoryStream(segment))
                            {
                                var buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    audioInputStream.Write(buffer, bytesRead);
                                }
                                audioInputStream.Close();
                            }
                        });

                        using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
                        {
                            var result = await recognizer.RecognizeOnceAsync();

                            if (result.Reason == ResultReason.RecognizedSpeech)
                            {
                                _logger.LogInformation("Segment {SegmentIndex}: Recognized text: {Text}", currentSegmentIndex, result.Text);

                                lock (transcription)
                                {
                                    transcription.Add((currentSegmentIndex, result.Text));
                                }
                            }
                            else if (result.Reason == ResultReason.NoMatch)
                            {
                                _logger.LogInformation("Segment {SegmentIndex}: No speech could be recognized.", currentSegmentIndex);
                            }
                            else if (result.Reason == ResultReason.Canceled)
                            {
                                var cancellation = CancellationDetails.FromResult(result);
                                _logger.LogError("Segment {SegmentIndex}: Speech recognition canceled. Reason: {Reason}, ErrorDetails: {ErrorDetails}", currentSegmentIndex, cancellation.Reason, cancellation.ErrorDetails);
                            }
                        }
                    }
                }));

                offset += length;
                segmentIndex++;
            }

            await Task.WhenAll(tasks);

            // Sort the transcriptions by segment index
            var orderedTranscriptions = transcription.OrderBy(t => t.Index).Select(t => t.Text);

            return string.Join(" ", orderedTranscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during speech recognition from byte array.");
            throw;
        }

    }
}