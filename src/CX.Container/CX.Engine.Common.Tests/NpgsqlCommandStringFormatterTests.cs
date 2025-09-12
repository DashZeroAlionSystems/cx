using CX.Engine.Common.PostgreSQL;

namespace CX.Engine.Common.Tests;

public class NpgsqlCommandStringFormatterTests
{
    [Fact]
    public async Task UnnamedArgs()
    {
        var cmd = await NpgsqlCommandStringFormatter.FormatAsync("SELECT * FROM cars WHERE reg_no = {answer.reg_no} AND roadworthy = {answer.roadworthy}", 
            new { answer = new { reg_no = "ABC123", roadworthy = true } });
        Assert.Equal("SELECT * FROM cars WHERE reg_no = @arg1 AND roadworthy = @arg2", cmd.CommandText);
        Assert.True(cmd.Parameters.Contains("arg1"));
        Assert.True(cmd.Parameters.Contains("arg2"));
        Assert.Equal("ABC123", cmd.Parameters["arg1"].Value);
        Assert.Equal(true, cmd.Parameters["arg2"].Value);
    }

    [Fact]
    public async Task NamedArgs()
    {
        var cmd = await NpgsqlCommandStringFormatter.FormatAsync("SELECT * FROM cars WHERE reg_no = {@reg_no:answer.reg_no} AND roadworthy = {@roadworthy:answer.roadworthy}", 
            new { answer = new { reg_no = "ABC123", roadworthy = true } });
        Assert.Equal("SELECT * FROM cars WHERE reg_no = @reg_no AND roadworthy = @roadworthy", cmd.CommandText);
        Assert.True(cmd.Parameters.Contains("reg_no"));
        Assert.True(cmd.Parameters.Contains("roadworthy"));
        Assert.Equal("ABC123", cmd.Parameters["reg_no"].Value);
        Assert.Equal(true, cmd.Parameters["roadworthy"].Value);
    }
}