using CX.Clients.Afriforum.Domain;
using CX.Engine.Archives.Pinecone;
using CX.Engine.Assistants.ContextAI;
using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common.DistributedLocks;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary.PostgreSQL;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.DocExtractors;
using CX.Engine.DocExtractors.Images;
using CX.Engine.DocExtractors.Text;
using CX.Engine.FileServices;
using CX.Engine.Importing;
using CX.Engine.TextProcessors;
using CX.Engine.TextProcessors.Splitters;
using CX.Engine.Assistants.Walter1;
using CX.Engine.Common;
using CX.Engine.Importing.Prod;
using CX.Engine.SharedOptions;

namespace CX.Engine.DeploymentHelper;

public static class JsonTplMerger
{
    public static void Merge(string outPath, string inPath, string sourcePath)
    {
        var tpl = new GoTemplateToJson();
        var source = tpl.Load(sourcePath);
        var target = tpl.Load(inPath);

        RemoveDeprecatedSections(
            "VectorLink1ImporterOptions",
            "Pinecone1Options",
            "OpenAIEmbedderOptions",
            "OpenAIChatOptions",
            "BruteChunk1Options",
            "AzureAITranslatorOptions",
            "PostgreSQLClientOptions",
            "ContextAIOptions",
            "LangfuseOptions",
            "JsonStoreOptions",
            "PythonDocXOptions",
            "PDFPlumberOptions",
            "PDFExtractOptions",
            "VectorLink1ImporterOptions",
            "EmbeddingCacheOptions",
            "OpenAIChatOptions",
            "Walter1AssistantOptions",
            "WeeleeAssistant"
        );
        SourceAuthoritySections(
            PineconeDI.ConfigurationSection,
            EmbedderDI.OpenAIEmbedderConfigurationSection,
            OpenAIChatAgentsDI.ConfigurationSection,
            LineSplitterDI.ConfigurationSection,
            AzureAITranslatorDI.ConfigurationSection,
            AzureContentSafetyDI.ConfigurationSection,
            PostgreSQLClientDI.ConfigurationSection,
            ContextAIDI.ConfigurationSection,
            LangfuseDI.ConfigurationSection,
            JsonStoreDI.ConfigurationSection,
            PythonProcessDI.ConfigurationSection,
            PythonDocXDI.ConfigurationSection,
            PDFPlumberDI.ConfigurationSection,
            DocXToPDFDI.ConfigurationSection,
            PDFToJpgDI.ConfigurationSection,
            PostgreSQLBinaryStoreDI.ConfigurationSection,
            Gpt4VisionExtractorDI.ConfigurationSection,
            VectorLinkImporterDI.ConfigurationSection,
            FileServiceDI.ConfigurationSection,
            EmbedderDI.EmbeddingCacheConfigurationSection,
            ChatCacheDI.ConfigurationSection,
            Walter1AssistantDI.ConfigurationSection,
            DistributedLockServiceDI.ConfigurationSection,
            SakenetwerkAssistant.ConfigurationSection,
            ConfigJsonStoreProviderDI.ConfigurationSection, 
            WeeleeDI.ConfigurationSection,
            StructuredDataDI.ConfigurationSection,
            PgConsoleDI.ConfigurationSection,
            LuaCoreDI.ConfigurationSection,
            ProdS3HelperDI.ConfigurationSection);

        tpl.Save(outPath, target);

        void RemoveDeprecatedSections(params string[] sections)
        {
            foreach (var section in sections)
                target.Remove(section);
        }

        void SourceAuthoritySections(params string[] sections)
        {
            foreach (var section in sections)
                SourceAuthoritySection(section);
        }

        void SourceAuthoritySection(string section)
        {
            target[section] = source[section]!.DeepClone();
        }
    }
}