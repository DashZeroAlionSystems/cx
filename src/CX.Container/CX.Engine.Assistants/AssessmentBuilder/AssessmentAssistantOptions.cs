using CX.Engine.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace CX.Engine.Assistants.AssessmentBuilder;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AssessmentAssistantOptions : IValidatableConfiguration
{
    public string ChatAgentName { get; set; }
    public string Document { get; set; }
    public string PostgreSQLClientName { get; set; } = "pg_default";
    public bool UseCrc32CachedChat { get; set; } = true;
    public string CacheTableName { get; set; } = "assessment_cache";

    public string DiskImportPath { get; set; } = "D:\\cx\\Clients\\SOS\\Assessment Builder\\DiskImport";
    public string ArchiveName { get; set; } = "pg-vector.sos_assessment_builder";
    public string EmbeddingCachePath { get; set; } = @"D:\CX\Clients\SOS\temp\sos-embeddings.cache";
    public int QuestionContextCutoffTokens { get; set; } = 9_000;
    public double QuestionContextMinSimilarity { get; set; } = 0.5;
    public string StorageService { get; set; }
    public string AssistantName { get; set; }
    public string StructureQuery { get; set; }
    public string IntroPrompt { get; set; }

    public void Validate(IConfigurationSection section)
    {
        section.ThrowIfNullOrWhiteSpace(ChatAgentName);
        section.ThrowIfNull(Document);
        section.ThrowIfNullOrWhiteSpace(ArchiveName);
        section.ThrowIfZeroOrNegative(QuestionContextCutoffTokens);
        section.ThrowIfNotInRange(QuestionContextMinSimilarity, 0.0, 1.0);
        section.ThrowIfNullOrWhiteSpace(AssistantName);
        section.ThrowIfNullOrWhiteSpace(StructureQuery);
        section.ThrowIfNullOrWhiteSpace(IntroPrompt);

        if (UseCrc32CachedChat)
        {
            section.ThrowIfNullOrWhiteSpace(PostgreSQLClientName,
                $"{nameof(PostgreSQLClientName)} is required when {nameof(UseCrc32CachedChat)} is true for {section.Path}");
            section.ThrowIfNullOrWhiteSpace(CacheTableName,
                $"{nameof(CacheTableName)} is required when {nameof(UseCrc32CachedChat)} is true for {section.Path}");
        }
    }
}