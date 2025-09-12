using CX.Engine.Common;
using CX.Engine.Common.Embeddings;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CX.Engine.DocExtractors;
using CX.Engine.TextProcessors.Splitters;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Importing.Tests;

public class VectorLinkImporterTests : TestBase
{
    private VectorLinkImporter _importer = null!;
    
    [Fact]
    public Task ImportTestDocumentToPineconeTest() => Builder.RunAsync(this, async () =>
    {
        var text = TestResourcesImporting.AbalexInsectGel();
        await _importer.ImportAsync(new()
        {
            SourceDocumentDisplayName = "Abalex Insect Gel Manual",
            DocumentId = Guid.NewGuid(),
            DocumentContent = text.AsMemoryStream(),
            ExtractImages = false,
            Tags = ["manual", "insecticide"]
        });
    });

    protected override void ContextReady(IServiceProvider sp)
    {
        _importer = sp.GetRequiredService<VectorLinkImporter>();
    }

    public VectorLinkImporterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(
            SecretNames.VectorLinkImporters.in_memory_3_large,
            SecretNames.LineSplitter._400,
            SecretNames.Pinecone.vectormind_test_1536,
            SecretNames.EmbeddingCache.None,
            SecretNames.DocXToPDF.DocXToPDF_disk,
            SecretNames.JsonStores.pg_local,
            SecretNames.PythonProcesses.Local,
            SecretNames.OpenAIEmbedder,
            SecretNames.DiskBinaryStores.Common,
            SecretNames.PostgreSQLBinaryStores.Default,
            SecretNames.PDFToJpg.PDFToJpg_disk,
            SecretNames.PostgreSQL.pg_local,
            SecretNames.InMemoryArchives);
        Builder.AddServices((sc, config) =>
        {
            sc.AddPostgreSQLClients(config);
            sc.AddPythonProcesses(config);
            sc.AddBinaryStores(config);
            sc.AddDocumentExtractors(config);
            sc.AddEmbeddings(config);
            sc.AddLineSplitter(config);
            sc.AddArchives(config);
            sc.AddJsonStores(config);
            sc.AddVectorLinkImporter(config);
        });
    }
}