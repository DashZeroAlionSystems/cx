using CX.Engine.Common;
using CXLibTests.Resources;

namespace CX.Engine.DocExtractors.Tests;

public class FileFormatCheckerTests
{
    [Fact]
    public async Task TextDetectionAsync()
    {
        Assert.True(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.UTF8_txt)));
        Assert.True(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.UTF8_BOM_txt)));
        Assert.True(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.UTF16_BE_BOM_txt)));
        Assert.True(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.UTF16_LE_BOM_txt)));
        Assert.True(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.withholding_csv)));
        Assert.False(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.This_is_a_test_pdf)));
        Assert.False(await FileFormatChecker.IsTextStreamAsync(this.GetResource(Resource.Word_docx)));

        // Test with a null-terminated Utf8 stream
        {
            using var ms = await this.GetResource(Resource.UTF8_txt).CopyToMemoryStreamAsync();
            ms.Position = ms.Length - 1;
            ms.WriteByte(0);
            ms.Position = 0;
            Assert.True(await FileFormatChecker.IsTextStreamAsync(ms));
        }
    }
}