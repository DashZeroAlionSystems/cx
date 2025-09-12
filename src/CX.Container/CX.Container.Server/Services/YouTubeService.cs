using NAudio.Wave;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace CX.Container.Server.Services;

public sealed class YouTubeService : IYouTubeService
{    
    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<YouTubeService> _logger;

    public YouTubeService(YoutubeClient youtubeClient, ILogger<YouTubeService> logger)
    {        
        _youtubeClient = youtubeClient;
        _logger = logger;
    }

    /// <summary>
    /// Downloads audio from a YouTube video, converts it to 16k PCM format, and returns it as a byte array.
    /// </summary>
    /// <param name="youTubeUrl">The URL of the YouTube video from which to download audio.</param>
    /// <returns>A byte array containing the audio data in 16k PCM format.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no audio stream is available from the YouTube video.</exception>
    public async Task<byte[]> GetYouTubeAudioAs16kPcmBytesAsync(string youTubeUrl)
    {
        try
        {
            _logger.LogInformation("Fetching YouTube video for URL: {YouTubeUrl}", youTubeUrl);

            var video = await _youtubeClient.Videos.GetAsync(youTubeUrl);

            _logger.LogInformation("Retrieving stream manifest for video ID: {VideoId}", video.Id);

            // Get the stream manifest for the video
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

            // Select the best audio stream available
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            if (audioStreamInfo == null)
            {
                _logger.LogError("No audio stream available for video ID: {VideoId}", video.Id);
                throw new InvalidOperationException("No audio stream available.");
            }

            _logger.LogInformation("Downloading audio stream with bitrate: {Bitrate}", audioStreamInfo.Bitrate);

            // Download the audio stream to a memory stream
            using var audioStream = await _youtubeClient.Videos.Streams.GetAsync(audioStreamInfo);
            using var inputStream = new MemoryStream();
            await audioStream.CopyToAsync(inputStream);
            inputStream.Position = 0;

            _logger.LogInformation("Converting audio to 16k PCM format");

            // Convert MP3 to PCM
            using var mediaReader = new StreamMediaFoundationReader(inputStream);
            using var pcmStream = new MemoryStream();
            var outFormat = new WaveFormat(16000, 16, 1);

            // Convert to PCM format
            using var resampler = new MediaFoundationResampler(mediaReader, outFormat)
            {
                ResamplerQuality = 60 // Set the resampling quality
            };
            WaveFileWriter.WriteWavFileToStream(pcmStream, resampler);

            _logger.LogInformation("Audio conversion completed for URL: {YouTubeUrl}", youTubeUrl);

            return pcmStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing audio for YouTube URL: {YouTubeUrl}", youTubeUrl);
            throw;
        }
    }

    /// <summary>
    /// Gets title from a YouTube video.
    /// </summary>
    /// <param name="youTubeUrl"></param>
    /// <returns>A string containing the title of the video.</returns>
    public async Task<string> GetYouTubeVideoTitleAsync(string youTubeUrl)
    {
        try
        {
            _logger.LogInformation("Fetching YouTube video for URL: {YouTubeUrl}", youTubeUrl);

            var video = await _youtubeClient.Videos.GetAsync(youTubeUrl);
            var videoTitle = video.Title;

            _logger.LogInformation("Retrieved video title: {VideoTitle}", videoTitle);

            // Remove or replace invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var regex = new Regex($"[{Regex.Escape(new string(invalidChars))}]");
            var sanitizedFileName = regex.Replace(videoTitle, " ");

            // Optionally, remove or replace any additional unwanted characters
            sanitizedFileName = sanitizedFileName.Replace(",", " ").Trim();
            sanitizedFileName = sanitizedFileName.Replace(".", "").Trim();
            sanitizedFileName = sanitizedFileName.Replace("#", "").Trim();

            // Remove hashtags and special characters, including emojis
            sanitizedFileName = Regex.Replace(sanitizedFileName, @"[^\w\s]", "");

            // Remove multiple spaces by replacing them with a single space
            sanitizedFileName = Regex.Replace(sanitizedFileName, @"\s+", " ");

            // Create the file name with extension
            var fileName = $"{sanitizedFileName}.txt";

            _logger.LogInformation("Generated sanitized file name: {FileName}", fileName);

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching video title for URL: {YouTubeUrl}", youTubeUrl);
            throw;
        }
    }
}