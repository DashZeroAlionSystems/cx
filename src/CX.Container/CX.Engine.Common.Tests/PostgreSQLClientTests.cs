using System.Data.Common;
using CX.Engine.Common.Db;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using JetBrains.Annotations;
using Npgsql;

namespace CX.Engine.Common.Tests;

public class PostgreSQLClientTests : TestBase
{
    private PostgreSQLClient _sql = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _sql = sp.GetRequiredNamedService<PostgreSQLClient>("pg_local");
    }

    [Fact]
    public async Task ExecuteTests() => await Builder.RunAsync(async () =>
    {
        //Parameterized with a command string
        await _sql.ExecuteAsync("SELECT 1");
        //Parameterized with a command
        await _sql.ExecuteAsync(new NpgsqlCommand("SELECT 1"));
        //Parameterized with a command interpolated string
        var val = 1;
        await _sql.ExecuteAsync($"SELECT {val}");
    });

    [Fact]
    public async Task ExecuteScalarTests() => await Builder.RunAsync(async () =>
    {
        //Parameterized with a command string
        var res = await _sql.ScalarAsync<int>("SELECT 1");
        Assert.Equal(1, res);

        //Parameterized with a command
        res = await _sql.ScalarAsync<int>(new NpgsqlCommand("SELECT 2"));
        Assert.Equal(2, res);

        //Parameterized with a command interpolated string
        var val = 3;
        res = await _sql.ScalarAsync<int>($"SELECT {val}");
        Assert.Equal(3, res);
    });

    private class TestBinaryStoreRow : IPopulateFromDbDataReader
    {
        public int Value { get; private set; }

        public void PopulateFromDbDataReader(DbDataReader reader)
        {
            Value = reader.Get<int>("val");
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AutoRow
    {
        public int Val;
    }

    [Fact]
    public async Task RowTests() => await Builder.RunAsync(async () =>
    {
        //Test with custom mappers
        {
            static int GetFirstColumnAsInt(DbDataReader r) => r.GetInt32(0);

            //Parameterized with a command string
            var res = await _sql.RowAsync("SELECT 1 AS val", GetFirstColumnAsInt);
            Assert.Equal(1, res);

            //Parameterized with a command
            res = await _sql.RowAsync(new NpgsqlCommand("SELECT 2 AS val"), GetFirstColumnAsInt);
            Assert.Equal(2, res);

            //Parameterized with a command interpolated string
            var val = 3;
            res = await _sql.RowAsync($"SELECT {val} AS val", GetFirstColumnAsInt);
            Assert.Equal(3, res);
        }

        //Test with IPopulateFromDbDataReader
        {
            //Parameterized with a command string
            var res = await _sql.RowAsync<TestBinaryStoreRow>("SELECT 1 AS val");
            Assert.Equal(1, res?.Value);

            //Parameterized with a command
            res = await _sql.RowAsync<TestBinaryStoreRow>(new NpgsqlCommand("SELECT 2 AS val"));
            Assert.Equal(2, res?.Value);

            //Parameterized with a command interpolated string
            var val = 3;
            res = await _sql.RowAsync<TestBinaryStoreRow>($"SELECT {val} AS val");
            Assert.Equal(3, res?.Value);
        }
    });

    [Fact]
    public async Task RowDapperTests() => await Builder.RunAsync(async () =>
    {
        //Parameterized with a command string
        var res = await _sql.RowDapperAsync<AutoRow>("SELECT 1 AS val");
        Assert.Equal(1, res?.Val);

        //Parameterized with a command
        res = await _sql.RowDapperAsync<AutoRow>(new NpgsqlCommand("SELECT 2 AS val"));
        Assert.Equal(2, res?.Val);

        //Parameterized with a command interpolated string
        var val = 3;
        res = await _sql.RowDapperAsync<AutoRow>($"SELECT {val} AS val");
        Assert.Equal(3, res?.Val);
    });

    [Fact]
    public async Task ListTests() => await Builder.RunAsync(async () =>
    {
        static int GetFirstColumnAsInt(DbDataReader r) => r.GetInt32(0);

        {
            //String input with custom mapper
            var res = await _sql.ListAsync("SELECT 1 AS Val UNION SELECT 2 AS Val", GetFirstColumnAsInt);
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0]);
            Assert.Equal(2, res[1]);
        }

        {
            //String input with IPopulateFromDbDataReader
            var res = await _sql.ListAsync<TestBinaryStoreRow>("SELECT 1 AS Val UNION SELECT 2 AS Val");
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0]?.Value);
            Assert.Equal(2, res[1]?.Value);
        }

        const int val1 = 1;
        const int val2 = 2;
        {
            //Interpolated input with custom mapper
            var res = await _sql.ListAsync($"SELECT {val1} AS Val UNION SELECT {val2} AS Val", GetFirstColumnAsInt);
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0]);
            Assert.Equal(2, res[1]);
        }

        {
            //Interpolated input with IPopulateFromDbDataReader
            var res = await _sql.ListAsync<TestBinaryStoreRow>($"SELECT {val1} AS Val UNION SELECT {val2} AS Val");
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0]?.Value);
            Assert.Equal(2, res[1]?.Value);
        }

        {
            //Command input with customer mapper
            var res = await _sql.ListAsync(new NpgsqlCommand("SELECT 1 AS Val UNION SELECT 2 AS Val"), GetFirstColumnAsInt);
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0]);
            Assert.Equal(2, res[1]);
        }
        
        {
            //Command input with IPopulateFromDbDataReader
            var res = await _sql.ListAsync<TestBinaryStoreRow>(new NpgsqlCommand("SELECT 1 AS Val UNION SELECT 2 AS Val"));
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0]?.Value);
            Assert.Equal(2, res[1]?.Value);
        }
    });
    
    [Fact]
    public Task ListDapperTests() => Builder.RunAsync(async () => {
        //With query
        var res = await _sql.ListDapperAsync<AutoRow>("SELECT 1 AS val UNION SELECT 2 AS val");
        Assert.Equal(2, res.Count);
        Assert.Equal(1, res[0]?.Val);
        Assert.Equal(2, res[1]?.Val);
        
        //With interpolated string
        const int val3 = 3;
        const int val4 = 4;
        res = await _sql.ListDapperAsync<AutoRow>($"SELECT {val3} AS val UNION SELECT {val4} AS val");
        Assert.Equal(2, res.Count);
        Assert.Equal(3, res[0]?.Val);
        Assert.Equal(4, res[1]?.Val);
        
        //With command
        res = await _sql.ListDapperAsync<AutoRow>(new NpgsqlCommand("SELECT 1 AS val UNION SELECT 2 AS val"));
        Assert.Equal(2, res.Count);
        Assert.Equal(1, res[0]?.Val);
        Assert.Equal(2, res[1]?.Val);
    });

    public PostgreSQLClientTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddConfig(SecretsProvider.Get(SecretNames.PostgreSQL.pg_local));
        Builder.AddServices((sc, config) => { sc.AddPostgreSQLClients(config); });
    }
}