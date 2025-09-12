namespace CX.Engine.Configuration;

public static class SecretNames
{
    public static class ScheduledQuestionAgents
    {
        public const string Local = "ScheduledQuestionAgent\\pg_local.json";
        public const string Weelee = "ScheduledQuestionAgent\\Weelee.json";
        public const string Afriforum = "ScheduledQuestionAgent\\Afriforum.json";
    }
    
    public static class Pinecone
    {
        public const string vectormind_test_1536 = "Pinecone\\vectormind_test_1536.json";
        public const string pinecone_default = "Pinecone\\default.json";
        public const string playground = "Pinecone\\playground.json";
        public const string prod = "Pinecone\\prod.json";
        public const string afriforum = "Pinecone\\afriforum.json";
        public const string andre_qa_small = "Pinecone\\andre_qa_small.json";
        public const string aela = "Pinecone\\aela.json";
        public const string large_prod = "Pinecone\\large-prod.json";
        public const string villa_prod = "Pinecone\\villa-prod.json";
        public const string sholto_test_3_large = "Pinecone\\sholto-test-3-large.json";
        public const string afriforum_large = "Pinecone\\afriforum-large.json";
    }

    public static class PostgreSQL
    {
        public const string pg_local = "PostgreSQL\\pg_local.json";
        public const string pg_local_sos = "PostgreSQL\\pg_local_sos.json";
        public const string pg_local_vector = "PostgreSQL\\pg_local_vector.json";
        public const string pg_local_gic = "PostgreSQL\\pg_local_gic.json";
        public const string pg_default = "PostgreSQL\\pg_default.json";
        public const string pg_local_weelee = "PostgreSQL\\pg_local_weelee.json";
        public const string pg_local_playground = "PostgreSQL\\pg_local_playground.json";
        public const string pg_universalfans = "PostgreSQL\\pg_universalfans.json";
        public const string discordserver_pg_local = "PostgreSQL\\discordserver_pg_local.json";
        public const string pg_afriforum_prod = "PostgreSQL\\pg_afriforum_prod.json";
        public const string pg_afriforum_local = "PostgreSQL\\pg_afriforum_local.json";
        public const string pg_playground = "PostgreSQL\\pg_playground.json";
        public const string pg_mccarthy = "PostgreSQL\\pg_mccarthy.json";
        public const string pg_gic = "PostgreSQL\\pg_gic.json";
    }

    public static class DistributedLockServices
    {
        public const string pg_local = "DistributedLockServices\\pg_local.json";
        public const string pg_playground = "DistributedLockServices\\pg_playground.json";
    }

    public static class ProdRepos
    {
        public const string prodrepo_playground = "ProdRepos\\prodrepo_playground.json";
        public const string prodrepo_afriforum_local = "ProdRepos\\prodrepo_afriforum_local.json";
    }

    public static class ProdS3Helpers
    {
        public const string prods3helper_afriforum = "ProdS3Helpers\\prods3helper_afriforum.json";
        public const string prods3helper_playground = "ProdS3Helpers\\prods3helper_playground.json";
    }

    public static class Clients
    {
        public const string Afriforum = "Clients\\Afriforum.json";
        public const string Weelee = "Clients\\Weelee.json";
    }

    public static class Langfuse
    {
        public const string Local = "Langfuse\\Local.json";
        public const string Disabled = "Langfuse\\Disabled.json";
        public const string Cloud = "Langfuse\\Cloud.json";
    }

    public static class EmbeddingCache
    {
        public const string None = "EmbeddingCaches\\None.json";
        public const string LocalDisk = "EmbeddingCaches\\LocalDisk.json";
        public const string Aela = "EmbeddingCaches\\Aela.json";
        public const string Britehouse = "EmbeddingCaches\\Britehouse.json";
        public const string Playground = "EmbeddingCaches\\Playground.json";
        public const string SOS = "EmbeddingCaches\\SOS.json";
        public const string ProdDemo = "EmbeddingCaches\\ProdDemo.json";
        public const string Villa = "EmbeddingCaches\\Villa.json";
        public const string DiscordServer = "EmbeddingCaches\\DiscordServer.json";
        public const string Afriforum = "EmbeddingCaches\\Afriforum.json";
    }

    public static class ChatCache
    {
        public const string LocalDisk = "ChatCaches\\LocalDisk.json";
        public const string Aela = "ChatCaches\\Aela.json";
        public const string Britehouse = "ChatCaches\\Britehouse.json";
        public const string Playground = "ChatCaches\\Playground.json";
        public const string SOS = "ChatCaches\\SOS.json";
        public const string ProdDemo = "ChatCaches\\ProdDemo.json";
        public const string Villa = "ChatCaches\\Villa.json";
        public const string DiscordServer = "ChatCaches\\DiscordServer.json";
        public const string Afriforum = "ChatCaches\\Afriforum.json";
    }

    public static class Walter1Assistants
    {
        //Sets
        public const string Aela = "Walter1Assistants\\Aela.json";
        public const string Britehouse = "Walter1Assistants\\Britehouse.json";
        public const string Villa = "Walter1Assistants\\Villa.json";
        public const string ProdDemo = "Walter1Assistants\\ProdDemo.json";

        //Instances
        public const string default_3_large = "Walter1Assistants\\default-3-large.json";
        public const string afriforum_local = "Walter1Assistants\\afriforum-local.json";
        public const string afriforum_pinecone = "Walter1Assistants\\afriforum-pinecone.json";
        public const string playground = "Walter1Assistants\\playground.json";
        public const string prod = "Walter1Assistants\\prod.json";
    }

    public static class DiskImporters
    {
        public const string Aela = "DiskImporters\\Aela.json";
        public const string Britehouse = "DiskImporters\\Britehouse.json";
        public const string SOS = "DiskImporters\\SOS.json";
        public const string Playground = "DiskImporters\\Playground.json";
        public const string ProdDemo = "DiskImporters\\ProdDemo.json";
        public const string Villa = "DiskImporters\\Villa.json";
        public const string DiscordLocal = "DiskImporters\\DiscordLocal.json";
        public const string DiscordServer = "DiskImporters\\DiscordServer.json";
        public const string Afriforum = "DiskImporters\\Afriforum.json";
    }

    public static class FileServices
    {
        public const string LocalDisk = "FileServices\\LocalDisk.json";
        public const string Aela = "FileServices\\Aela.json";
        public const string Playground = "FileServices\\Playground.json";
        public const string SOS = "FileServices\\SOS.json";
        public const string ProdDemo = "FileServices\\ProdDemo.json";
        public const string Britehouse = "FileServices\\Britehouse.json";
        public const string Villa = "FileServices\\Villa.json";
        public const string DiscordServer = "FileServices\\DiscordServer.json";
        public const string Afriforum = "FileServices\\Afriforum.json";
    }

    public static class VectormindLiveAssistants
    {
        public const string playground = "VectormindLiveAssistants\\playground.json";
        public const string afriforum = "VectormindLiveAssistants\\afriforum.json";
        public const string britehouse = "VectormindLiveAssistants\\britehouse.json";
        public const string prod = "VectormindLiveAssistants\\prod.json";
        public const string villa = "VectormindLiveAssistants\\villa.json";
    }

    public static class VectormindProdImporters
    {
        public const string Afriforum = "VectormindProdImporters\\Afriforum.json";
        public const string Playground = "VectormindProdImporters\\Playground.json";
    }

    public static class DiskBinaryStores
    {
        public const string Common = "DiskBinaryStores\\Common.json";
        public const string Aela = "DiskBinaryStores\\Aela.json";
        public const string Britehouse = "DiskBinaryStores\\Britehouse.json";
        public const string ProdDemo = "DiskBinaryStores\\ProdDemo.json";
        public const string Villa = "DiskBinaryStores\\Villa.json";
        public const string DiscordServer = "DiskBinaryStores\\DiscordServer.json";
        public const string Afriforum = "DiskBinaryStores\\Afriforum.json";
    }

    public static class ContextAI
    {
        public const string Enabled = "ContextAI\\Enabled.json";
        public const string Disabled = "ContextAI\\Disabled.json";
    }

    public static class AutoQA
    {
        public const string Aela = "AutoQA\\Aela.json";
        public const string Playground = "AutoQA\\Playground.json";
        public const string ProdDemo = "AutoQA\\ProdDemo.json";
        public const string Britehouse = "AutoQA\\Britehouse.json";
        public const string Villa = "AutoQA\\Villa.json";
        public const string Afriforum = "AutoQA\\Afriforum.json";
    }

    public static class LineSplitter
    {
        public const string _400 = "LineSplitters\\400.json";
        public const string _800 = "LineSplitters\\800.json";
        public const string _1200 = "LineSplitters\\1200.json";
        public const string _4000 = "LineSplitters\\4000.json";
    }

    public static class Discord
    {
        public const string Local = "Discord\\Local.json";
        public const string DiscordServer = "Discord\\DiscordServer.json";
    }

    public static class PythonProcesses
    {
        public const string Local = "PythonProcesses\\Local.json";
        public const string DiscordServer = "PythonProcesses\\DiscordServer.json";
    }

    public static class MoonyConsoleServices
    {
        public const string Aela = "MoonyConsoleServices\\Aela.json";
        public const string Britehouse = "MoonyConsoleServices\\Britehouse.json";
        public const string Playground = "MoonyConsoleServices\\Playground.json";
        public const string GIC = "MoonyConsoleServices\\GIC.json";
        public const string SOS = "MoonyConsoleServices\\SOS.json";
        public const string ProdDemo = "MoonyConsoleServices\\ProdDemo.json";
        public const string Villa = "MoonyConsoleServices\\Villa.json";
        public const string Afriforum = "MoonyConsoleServices\\Afriforum.json";
        public const string Weelee = "MoonyConsoleServices\\Weelee.json";
    }

    public static class RegistrationServices
    {
        public const string Aela = "RegistrationServices\\Aela.json";
        public const string Britehouse = "RegistrationServices\\Britehouse.json";
        public const string ProdDemo = "RegistrationServices\\ProdDemo.json";
        public const string Villa = "RegistrationServices\\Villa.json";
        public const string Afriforum = "RegistrationServices\\Afriforum.json";
        public const string Weelee = "RegistrationServices\\Weelee.json";
    }

    public static class VectorLinkImporters
    {
        public const string Playground = "VectorLinkImporters\\Playground.json";
        public const string in_memory_3_large = "VectorLinkImporters\\in-memory-3-large.json";
    }

    public static class DocXToPDF
    {
        public const string DocXToPDF_disk = "DocXToPDF\\DocXToPDF_disk.json";
        public const string DocXToPDF_pg = "DocXToPDF\\DocXToPDF_pg.json";
    }

    public static class PDFToJpg
    {
        public const string PDFToJpg_disk = "PDFToJpg\\PDFToJpg_disk.json";
        public const string PDFToJpg_pg = "PDFToJpg\\PDFToJpg_pg.json";
    }

    public static class PDFPlumber
    {
        public const string PDFPlumber_disk = "PDFPlumber\\PDFPlumber_disk.json";
        public const string PDFPlumber_pg = "PDFPlumber\\PDFPlumber_pg.json";
    }

    public static class PythonDocX
    {
        public const string PythonDocX_disk = "PythonDocX\\PythonDocX_disk.json";
        public const string PythonDocX_pg = "PythonDocX\\PythonDocX_pg.json";
    }

    public static class PostgreSQLBinaryStores
    {
        public const string Default = "PostgreSQLBinaryStores\\Default.json";
        public const string Playground = "PostgreSQLBinaryStores\\Playground.json";
    }

    public static class JsonStores
    {
        public const string pg_local = "JsonStores\\pg_local.json";
        public const string pg_playground = "JsonStores\\pg_playground.json";
    }

    public static class ConfigJsonStoreProviders
    {
        public const string Local = "ConfigJsonStoreProviders\\Local.json";
    }

    public static class LuaCores
    {
        public const string lua_default = "LuaCores\\default.json";
        public const string Afriforum = "LuaCores\\Afriforum.json";
        public const string Weelee = "LuaCores\\Weelee.json";
    }

    public const string OpenAIEmbedder = "OpenAIEmbedder.json";
    public const string OpenAIChatAgents = "OpenAIChatAgents.json";
    public const string AzureAITranslators = "AzureAITranslators.json";
    public const string AzureContentSafety = "AzureContentSafety.json";
    public const string GPT4VisionExtractor = "GPT4VisionExtractor.json";
    public const string InMemoryArchives = "InMemoryArchives.json";
    public const string PgVectorArchives = "PgVectorArchives.json";
    public const string MSDocAnalyzer = "MsDocAnalyzer.json";
    public const string PgConsole = "PgConsole.json";
    public const string GeminiChatAgents = "GeminiChatAgents.json";
    public const string VoiceTranscript = "VoiceTranscript.json";

    public const string UniversalFans = "UniversalFans.json";
}