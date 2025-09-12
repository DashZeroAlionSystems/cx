using CX.Engine.Archives;
using CX.Engine.Assistants.ContextAI;
using CX.Engine.ChatAgents;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Python;
using CX.Engine.Common.RegistrationServices;
using CX.Engine.Common.Stores;
using CX.Engine.Common.Stores.Json;
using CX.Engine.DocExtractors;
using CX.Engine.FileServices;
using CX.Engine.Importers;
using CX.Engine.Importing;
using CX.Engine.TextProcessors;
using CX.Engine.TextProcessors.Splitters;
using CX.Engine.Assistants;
using CX.Engine.Assistants.ArtifactAssists;
using CX.Engine.Assistants.Channels;
using CX.Engine.Assistants.PgTableEnrichment;
using CX.Engine.Assistants.ScheduledLua;
using CX.Engine.Assistants.ScheduledQuestions;
using CX.Engine.CallAnalysis;
using CX.Engine.CognitiveServices.ConversationAnalysis;
using CX.Engine.CognitiveServices.LanguageDetection;
using CX.Engine.CognitiveServices.SentimentAnalysis;
using CX.Engine.CognitiveServices.ToxicityAnalysis;
using CX.Engine.CognitiveServices.VoiceTranscripts;
using CX.Engine.Common;
using CX.Engine.Common.ACL;
using CX.Engine.Common.Storage;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.Migrations;
using CX.Engine.Common.Stores.Graphs;
using CX.Engine.Common.Telemetry;
using CX.Engine.QAndA.Auto;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Discord;
using CX.Engine.Importing.Prod;
using CX.Engine.QAndA;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Registration.Full;

public static class FullProfile
{
    public static void Register(bool migrations = false)
    {
        RegistrationService.StartupTasks.Add(static async host =>
        {
            await host.Services.GetRequiredService<ConfigJsonStoreProvider>().Loaded;
        });
        
        RegistrationService.ConfigureServices += (sc, configuration) =>
        {
            sc.AddJsonSchemaStore(configuration);
            sc.AddMigrationRunner();
            sc.AddConfigJsonStoreProvider(configuration);
            sc.AddJsonObjectStores(configuration);
            sc.AddJsonEdgeStores(configuration);
            sc.AddProdS3Helpers(configuration);
            sc.AddPgConsole(configuration);
            sc.AddLuaCore(configuration);
            sc.AddChannels(configuration);
            sc.AddDistributedLockService(configuration);
            sc.AddRegistrationService(configuration);
            sc.AddFileService(configuration);
            sc.AddDiskImporter(configuration);
            sc.AddVectormindProdImporter(configuration);
            sc.AddVectorLinkImporter(configuration);
            sc.AddDocumentExtractors(configuration);
            sc.AddChatAgents(configuration);
            sc.AddChatCache(configuration);
            sc.AddSqlServerClients(configuration);
            sc.AddPostgreSQLClients(configuration);
            sc.AddPythonProcesses(configuration);
            sc.AddStores(configuration);
            sc.AddLineSplitter(configuration);
            sc.AddEmbeddings(configuration);
            sc.AddContextAI(configuration);
            sc.AddLangfuse(configuration);
            sc.AddArchives(configuration);
            sc.AddAssistants(configuration);
            sc.AddTextProcessors(configuration);
            sc.AddAutoQAs(configuration);
            sc.AddPgTelemetryRecorder(configuration);
            sc.AddScheduledQuestionAgent(configuration);
            sc.AddScheduledLuaAgent(configuration);
            sc.AddDiscordServices(configuration);
            sc.AddACLService(configuration);
            sc.AddSingleton<EnvMetrics>();
            sc.AddArtifactAssist();
            sc.AddQueryCache(configuration);
            sc.AddQAServices(configuration);
            sc.AddTranscriptionServices(configuration);
            sc.AddConversationAnalyzers(configuration);
            sc.AddLanguageDetectors(configuration);
            sc.AddToxicityAnalyzers(configuration);
            sc.AddSentimentAnalyzers(configuration);
            sc.AddCallAnalyzers(configuration);
            sc.AddPgTableEnrichmentAssistant(configuration);
            sc.AddStorageServices(configuration);

            if (migrations)
                sc.AddFullProfileMigrations();
        };
    }
}