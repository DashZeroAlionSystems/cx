using CX.Engine.Common;
using CX.Engine.Common.PostgreSQL;
using JetBrains.Annotations;

namespace CX.Engine.Importing.Prod;

public class ProdRepo
{
    public readonly ProdRepoOptions Options;
    private readonly PostgreSQLClient _sql;

    public ProdRepo(ProdRepoOptions options, IServiceProvider sp)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>(Options.PostgreSQLClientName);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class SourceDocument
    {
        public Guid Id;
        public string DisplayName = null!;
        public string AwsFileName;
        public Func<Task<Stream>> GetContentAsync = null!;
        public List<Citation> Citations;
    }
    
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Citation
    {
        public Guid Id;
        public Guid SourceDocumentId = Guid.Empty;
        public string Name = null!;
        public string Url = null!;
        public byte[] Content;
    }
    
    public async Task SetDocumentOcrTextAsync(Guid documentId, string content)
    {
        await _sql.ExecuteAsync(
            $"""
            UPDATE source_documents
            SET ocr_text = {content}
            WHERE id = {documentId}
            """);
    }

    public async Task SetCitationOcrTextAsync(Guid documentId, string content)
    {
        await _sql.ExecuteAsync(
            $"""
             UPDATE citations
             SET ocr_text = {content}
             WHERE id = {documentId}
             """);
    }

    public async Task SetCitationDecoratorTextAsync(Guid documentId, string content)
    {
        await _sql.ExecuteAsync(
            $"""
             UPDATE citations
             SET decorator_text = {content}
             WHERE id = {documentId}
             """);
    }

    public async Task SetCitationImportWarningsAsync(Guid documentId, string content)
    {
        await _sql.ExecuteAsync(
            $"""
             UPDATE citations
             SET import_warnings = {content}
             WHERE id = {documentId}
             """);
    }

    public async Task SetDocumentDecoratorTextAsync(Guid documentId, string content)
    {
        await _sql.ExecuteAsync(
            $"""
             UPDATE source_documents
             SET decorator_text = {content}
             WHERE id = {documentId}
             """);
    }
    
    public async Task SetDocumentImportWarningsTextAsync(Guid documentId, string content)
    {
        await _sql.ExecuteAsync(
            $"""
             UPDATE source_documents
             SET import_warnings = {content}
             WHERE id = {documentId}
             """);
    }
    public async Task<List<SourceDocument>> GetTrainedSourceDocumentsAsync()
    {
        var docs = await _sql.ListDapperAsync<SourceDocument>(
            """
            SELECT id Id, display_name DisplayName, name AwsFileName
            FROM source_documents 
            WHERE (status ILIKE '%done%' OR status ILIKE '%error%') 
              AND coalesce(is_deleted, false) = false
              AND display_name IS NOT NULL
            """);
        
        if (docs.Any(doc => doc == null))
            throw new InvalidOperationException($"{nameof(docs)} contains nulls");

        return docs!;
    }

    public async Task<List<Citation>> GetAllCitationsAsync()
    {
        var citations = await _sql.ListDapperAsync<Citation>(
            """
            SELECT source_document_id SourceDocumentId, name Name, url Url, id Id, content Content 
            FROM citations c
            WHERE coalesce(is_deleted, false) = false 
              AND content IS NOT NULL
            """);

        if (citations.Any(citation => citation == null))
            throw new InvalidOperationException($"{nameof(citations)} contains nulls");

        return citations!;
    }
}