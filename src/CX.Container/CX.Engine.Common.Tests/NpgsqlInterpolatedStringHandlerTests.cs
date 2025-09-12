using CX.Engine.Common.PostgreSQL;
using Npgsql;

namespace CX.Engine.Common.Tests;

public class NpgsqlInterpolatedStringHandlerTests
{
    private NpgsqlCommand GetCommand(NpgsqlCommandInterpolatedStringHandler cmdString) => cmdString.GetCommand();

    [Fact]
    public void NpgsqlCommandInterpolatedStringHandlerTests()
    {
        //Test argument injection
        var cmd = GetCommand($"SELECT {1}");
        Assert.Equal("SELECT @1", cmd.CommandText);
        Assert.Single(cmd.Parameters);
        Assert.Equal(1, cmd.Parameters["@1"].Value);

        //Test formatted arguments handled as raw
        cmd = GetCommand($"SELECT {1.1234:0.00}");
        Assert.Equal("SELECT 1.12", cmd.CommandText);
        Assert.Empty(cmd.Parameters);

        //Test raw injection
        cmd = GetCommand($"{new InjectRaw("SELECT 4")}");
        Assert.Equal("SELECT 4", cmd.CommandText);
        Assert.Empty(cmd.Parameters);
        
        //Test deduplication
        cmd = GetCommand($"SELECT {1}, {1}, {2}");
        Assert.Equal("SELECT @1, @1, @2", cmd.CommandText);
        Assert.Equal(2, cmd.Parameters.Count);
        
    }
}