using CX.Engine.Assistants.ContextAI;
using CX.Engine.Common;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Testing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Configuration;
using CX.Engine.DocExtractors.Images;
using CX.Engine.FileServices;
using CX.Engine.Assistants;
using CX.Engine.Assistants.VectorMind;
using Xunit;
using Xunit.Abstractions;

namespace CXLibTests;

public class VectormindLiveAssistantTests : TestBase
{
    private VectormindLiveAssistant _assistant = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _assistant = sp.GetRequiredNamedService<VectormindLiveAssistant>("playground");
    }

    [Fact]
    public Task AuthorizeTest() => Builder.RunAsync(async () =>
    {
        var token = await _assistant.GetAccessTokenAsync();
        Assert.NotNull(token);
    });

    [Fact]
    public Task AskTest() => Builder.RunAsync(async () =>
    {
        var answer = await _assistant.AskAsync("What is the capital of the Free State in South Africa?",
            AgentRequest.NoHistoryTest());
        Assert.Contains("Bloemfontein", answer.Answer);
    });

    public VectormindLiveAssistantTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(
            SecretNames.VectormindLiveAssistants.playground, 
            SecretNames.PDFToJpg.PDFToJpg_disk, 
            SecretNames.PythonProcesses.Local, 
            SecretNames.JsonStores.pg_local, 
            SecretNames.PostgreSQL.pg_local,
            SecretNames.PostgreSQLBinaryStores.Default,
            SecretNames.DiskBinaryStores.Common,
            SecretNames.FileServices.LocalDisk,
            SecretNames.Langfuse.Disabled,
            SecretNames.ContextAI.Disabled);
        Builder.AddServices((sc, config) =>
        {
            sc.AddLangfuse(config);
            sc.AddContextAI(config);
            sc.AddFileService(config);
            sc.AddPythonProcesses(config);
            sc.AddJsonStores(config);
            sc.AddPDFToJpg(config);
            sc.AddPostgreSQLClients(config);
            sc.AddBinaryStores(config);
            sc.AddVectormindLiveAssistants(config);
        });
    }
}