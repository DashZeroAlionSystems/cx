using CX.Engine.Common;

namespace CX.Engine.Importing.Tests;

public static class TestResourcesImporting
{
    public static string AbalexInsectGel() => typeof(VectorLinkImporterTests).Assembly.GetEmbeddedResourceAsString("Abalex Insect Gel.md");
}