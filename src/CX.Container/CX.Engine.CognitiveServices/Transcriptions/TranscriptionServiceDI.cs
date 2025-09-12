using CX.Engine.CognitiveServices.Blobs;
using CX.Engine.CognitiveServices.ConversationAnalysis;
using CX.Engine.CognitiveServices.LanguageDetection;
using CX.Engine.CognitiveServices.SentimentAnalysis;
using CX.Engine.CognitiveServices.ToxicityAnalysis;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CX.Engine.CognitiveServices.VoiceTranscripts;

public static class TranscriptionServiceDI
{
    public const string ConfigurationSection = "TranscriptionServices";
    public const string ConfigurationTableName = "config_transcription_services";
    
    public static void AddTranscriptionServices(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<TranscriptionServiceOptions>(configuration, ConfigurationSection, ConfigurationTableName);

        sc.AddNamedSingletons<TranscriptionService>(configuration, static (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;

            var monitor = config.MonitorRequiredSection(ConfigurationSection, name, JsonOptionsSetup<TranscriptionServiceOptions>.Factory);
            var logger = sp.GetLogger<TranscriptionService>();

            return new(name, monitor, logger, sp);
        });
    }
}