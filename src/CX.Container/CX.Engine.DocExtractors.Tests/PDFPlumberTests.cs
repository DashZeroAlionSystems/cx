using CX.Engine.Common;
using CX.Engine.Common.Python;
using CX.Engine.Common.Stores.Binary;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using CX.Engine.DocExtractors.Text;
using CXLibTests.Resources;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CXLibTests;

public class PDFPlumberTests : TestBase
{
    private PDFPlumber _pdfPlumber = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _pdfPlumber = sp.GetRequiredService<PDFPlumber>();
    }

    [Fact]
    public Task ExtractPDFTest() => Builder.RunAsync(async () =>
    {
        var s = await _pdfPlumber.ExtractToTextAsync(this.GetResource(Resource.This_is_a_test_pdf), new());
        Assert.Contains("This is a test", s);
    });

    public PDFPlumberTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.PDFPlumber.PDFPlumber_disk, SecretNames.DiskBinaryStores.Common, SecretNames.PythonProcesses.Local);
        Builder.AddServices((sc, config) =>
        {
            sc.AddPythonProcesses(config);
            sc.AddBinaryStores(config);
            sc.AddPdfPlumber(config);
        });
    }
}