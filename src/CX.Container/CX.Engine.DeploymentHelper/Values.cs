using CX.Engine.HelmTemplates.Yaml;

namespace CX.Engine.DeploymentHelper;

public static class Values
{
    public static readonly YamlMap Map = new();

    public static readonly YamlValue OpenAIApiKey;
    public static readonly YamlValue AzureAIKey;
    public static readonly YamlValue PostgreSQLConnectionString;
    public static readonly YamlValue AzureContentSafetyApiKey;
    public static readonly YamlValue AzureContentSafetyEndpoint;
    public static readonly YamlValue ContextAIHost;
    public static readonly YamlValue ContextAIApiKey;
    public static readonly YamlValue LangfuseHost;
    public static readonly YamlValue LangfusePublicKey;
    public static readonly YamlValue LangfuseSecretKey;
    public static readonly YamlValue LangfuseTraceImports;
    public static readonly YamlValue PineconeApiKey;
    public static readonly YamlValue PineconeIndexName;
    public static readonly YamlValue PineconeNamespace;
    public static readonly YamlValue PineconeEmbeddingModel;
    public static readonly YamlValue AttachmentsBaseUrl;
    public static readonly YamlValue Walter1AssistantChatAgent;
    public static readonly YamlValue Walter1MinSimilarity;
    public static readonly YamlValue Walter1CutoffContextTokens;
    public static readonly YamlValue Walter1CutoffHistoryTokens;
    public static readonly YamlValue Walter1DefaultSystemPrompt;
    public static readonly YamlValue Walter1DefaultContextualizePrompt;
    public static readonly YamlValue Walter1TextProcessor1;
    public static readonly YamlValue Walter1TextProcessor2;

    public static readonly YamlValue SegmentTokenLimit;
    public static readonly YamlValue VectorLink_ExtractImages;
    public static readonly YamlValue VectorLink_TextProcessor1;
    public static readonly YamlValue VectorLink_TrainCitations;
    public static readonly YamlValue VectorLink_PreferImageTextExtraction;
    public static readonly YamlValue VectorLink_AttachToSelf;
    public static readonly YamlValue VectorLink_MaxConcurrency;
    public static readonly YamlValue VectorLink_DefaultAttachPageImages;

    public static readonly YamlValue Gpt4Vision_SystemPrompt;
    public static readonly YamlValue Gpt4Vision_Question;

    public static readonly YamlValue StuckturedDataApiKey;
    public static readonly YamlValue StuckturedDataApiSecret;

    public static readonly YamlValue WeeleeClientId;
    public static readonly YamlValue WeeleeClientSecret;
    public static readonly YamlValue WeeleeUsername;
    public static readonly YamlValue WeeleePassword;
    public static readonly YamlValue WeeleeRequestUrl;

    public static readonly YamlValue S3Region;
    public static readonly YamlValue S3PrivateBucket;
    public static readonly YamlValue S3PublicBucket;
    public static readonly YamlValue S3SecretAccessKey;
    public static readonly YamlValue S3AccessKeyId;
    public static readonly YamlValue S3Session;
    
    static Values()
    {
        VectorLink_ExtractImages = new(Map, "Config.VectorLinkImporter.ExtractImages", Vars.IMPORTER_EXTRACT_IMAGES.Hashbrace(), true);
        VectorLink_TextProcessor1 = new(Map, "Config.VectorLinkImporter.TextProcessor1", Vars.IMPORTER_TEXT_PROCESSOR_1.Hashbrace(), true);
        VectorLink_TrainCitations = new(Map, "Config.VectorLinkImporter.TrainCitations", Vars.IMPORTER_TRAIN_CITATIONS.Hashbrace(), true);
        VectorLink_AttachToSelf = new(Map, "Config.VectorLinkImporter.AttachToSelf", Vars.IMPORTER_DEFAULT_ATTACH_TO_SELF.Hashbrace(), true);
        VectorLink_DefaultAttachPageImages = new(Map, "Config.VectorLinkImporter.DefaultAttachPageImages", Vars.IMPORTER_DEFAULT_ATTACH_PAGE_IMAGES.Hashbrace(), true);
        VectorLink_PreferImageTextExtraction = new(Map, "Config.VectorLinkImporter.PreferImageTextExtraction", Vars.IMPORTER_PREFER_IMAGE_TEXT_EXTRACTION.Hashbrace(), true);
        VectorLink_MaxConcurrency = new(Map, "Config.VectorLinkImporter.MaxConcurrency", "1", false);
        
        Gpt4Vision_SystemPrompt = new(Map, "Config.Gpt4Vision.SystemPrompt", Vars.GPT4_VISION_SYSTEM_PROMPT.Hashbrace(), true);
        Gpt4Vision_Question = new(Map, "Config.Gpt4Vision.Question", Vars.GPT4_VISION_QUESTION.Hashbrace(), true);

        OpenAIApiKey = new(Map, "Config.OpenAiServer.ApiKey", Vars.OPENAI_API_KEY.Hashbrace(), true);
        AzureAIKey = new(Map, "Config.AzureAITranslator.ApiKey", Vars.AZURE_TRANSLATOR_API_KEY.Hashbrace(), true);
        PostgreSQLConnectionString = new(Map, "Config.Data.ConnectionString",
            $"Server={Vars.POSTGRESQL_DB_SERVER.Hashbrace()};Database=dashboard-server-{Vars.ENVIRONMENT.Hashbrace()};User Id={Vars.POSTGRESQL_DB_USER.Hashbrace()};Password={Vars.POSTGRESQL_DB_PASSWORD.Hashbrace()};Maximum Pool Size=300;Write Buffer Size=80000;",
            true);
        AzureContentSafetyApiKey = new(Map, "Config.AzureContentSafety.ApiKey", Vars.AZURE_CONTENT_SAFETY_API_KEY.Hashbrace(), true);
        AzureContentSafetyEndpoint = new(Map, "Config.AzureContentSafety.Endpoint", Vars.AZURE_CONTENT_SAFETY_ENDPOINT.Hashbrace(), true);
        ContextAIHost = new(Map, "Config.ContextAI.Host", Vars.CONTEXTAI_HOST.Hashbrace(), true);
        ContextAIApiKey = new(Map, "Config.ContextAI.ApiKey", Vars.CONTEXTAI_API_KEY.Hashbrace(), true);
        LangfuseHost = new(Map, "Config.Langfuse.Host", Vars.LANGFUSE_HOST.Hashbrace(), true);
        LangfusePublicKey = new(Map, "Config.Langfuse.PublicKey", Vars.LANGFUSE_PUBLIC_KEY.Hashbrace(), true);
        LangfuseSecretKey = new(Map, "Config.Langfuse.SecretKey", Vars.LANGFUSE_SECRET_KEY.Hashbrace(), true);
        LangfuseTraceImports = new(Map, "Config.Langfuse.TraceImports", Vars.LANGFUSE_TRACE_IMPORTS.Hashbrace(), true);

        PineconeApiKey = new(Map, "Config.Pinecone.ApiKey", Vars.PINECONE_API_KEY.Hashbrace(), true);
        PineconeIndexName = new(Map, "Config.Pinecone.IndexName", Vars.PINECONE_INDEX_NAME.Hashbrace(), true);
        PineconeNamespace = new(Map, "Config.Pinecone.Namespace", Vars.PINECONE_NAMESPACE.Hashbrace(), true);
        PineconeEmbeddingModel = new(Map, "Config.Pinecone.EmbeddingModel",
            Vars.EMBEDDINGS_MODEL_NAME.Hashbrace(), true);
        AttachmentsBaseUrl = new(Map, "Config.Attachments.BaseUrl", Vars.ARCHIVE_ATTACHMENTS_BASE_URL.Hashbrace(), true);
        SegmentTokenLimit = new(Map, "Config.LineSplitter.SegmentTokenLimit", Vars.LINE_SPLITTER_SEGMENT_TOKEN_LIMIT.Hashbrace(), false);

        Walter1AssistantChatAgent = new(Map, "Config.Walter1Assistant.ChatAgent",
            AppSettingsBuilder.ChatAgent_OpenAI_gpt_4o_mini, true);
        Walter1MinSimilarity = new(Map, "Config.Walter1Assistant.MinSimilarity", Vars.ASSISTANT_MIN_SIMILARITY.Hashbrace(), false);
        Walter1CutoffContextTokens = new(Map, "Config.Walter1Assistant.CutoffContextTokens", Vars.ASSISTANT_CUTOFF_CONTEXT_TOKENS.Hashbrace(), false);
        Walter1CutoffHistoryTokens = new(Map, "Config.Walter1Assistant.CutoffHistoryTokens", "9000", false);
        Walter1DefaultSystemPrompt = new(Map, "Config.Walter1Assistant.DefaultSystemPrompt", Vars.ASSISTANT_CHAT_PROMPT.Hashbrace(), true);
        Walter1DefaultContextualizePrompt = new(Map, "Config.Walter1Assistant.DefaultContextualizePrompt", Vars.ASSISTANT_CONTEXTUALIZE_PROMPT.Hashbrace(), true);

        Walter1TextProcessor1 = new(Map, "Config.Walter1Assistant.TextProcessor1",
             Vars.ASSISTANT_TEXT_PROCESSOR_1.Hashbrace(), true);
        Walter1TextProcessor2 = new(Map, "Config.Walter1Assistant.TextProcessor2",
             Vars.ASSISTANT_TEXT_PROCESSOR_2.Hashbrace(), true);

        StuckturedDataApiKey = new(Map, "Config.StructuredData.ApiKey", Vars.STUCTURED_DATA_API_KEY.Hashbrace(), true);
        StuckturedDataApiSecret = new(Map, "Config.StructuredData.ApiSecret", Vars.STUCTURED_DATA_API_SECRET.Hashbrace(), true);

        WeeleeClientId = new(Map, "Config.Weelee.ClientId", Vars.WEELEE_CLIENT_ID.Hashbrace(), true);
        WeeleeClientSecret = new(Map, "Config.Weelee.ClientSecret", Vars.WEELEE_CLIENT_SECRET.Hashbrace(), true);
        WeeleeUsername = new(Map, "Config.Weelee.Username", Vars.WEELEE_USERNAME.Hashbrace(), true);
        WeeleePassword = new(Map, "Config.Weelee.Password", Vars.WEELEE_PASSWORD.Hashbrace(), true);
        WeeleeRequestUrl = new(Map, "Config.Weelee.RequestUrl", Vars.WEELEE_REQUEST_URL.Hashbrace(), true);
        
        S3SecretAccessKey = new(Map, "Config.BucketStorage.AwsAccessKey", "sourced externally", true);
        S3PrivateBucket = new(Map, "Config.BucketStorage.PrivateBucketName", "sourced externally", true);
        S3PublicBucket = new(Map, "Config.BucketStorage.PublicBucketName", "sourced externally", true);
        S3AccessKeyId = new(Map, "Config.BucketStorage.AwsAccessKeyId", "sourced externally", true);
        S3Region = new(Map, "Config.BucketStorage.AwsRegion", "sourced externally", true);
        S3Session = new(Map, "Config.BucketStorage.AwsSession", "sourced externally", true);
    }
}