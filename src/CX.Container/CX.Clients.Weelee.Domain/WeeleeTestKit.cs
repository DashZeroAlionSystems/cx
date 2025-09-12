using CX.Engine.Assistants.Channels;
using CX.Engine.Assistants.FlatQuery;
using CX.Engine.Common;
using CX.Engine.Common.PostgreSQL;
using CX.Engine.Common.RegistrationServices;
using CX.Engine.QAndA.Structured;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Flurl.Util;
using Task = System.Threading.Tasks.Task;

namespace CX.Clients.Weelee.Domain;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class WeeleeTestKit : ILuaCoreLibrary
{
    private readonly IServiceProvider _sp;
    private readonly object _consoleLock = new();

    public WeeleeTestKit(IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public async Task<double> RunSet1Async(LuaInstance lua, string channelName = "weelee-test-1", string relation = "vweelee_stock_items_test_1", params string[] filter)
    {
        var channel = _sp.GetRequiredNamedService<Channel>(channelName);

        var run = new StructuredTestRunner<TestKit1Root>(
            _sp.GetRequiredNamedService<PostgreSQLClient>("pg_weelee"),
            (FlatQueryAssistant)channel.Assistant,
            lua.PrintLine);

        run.Tests.AddRange([
            new()
            {
                Name = "Make-Filter-Benz",
                Question = "any benz?",
                GetScoreAsync = async test =>
                {
                    // Example: SetSimilarity approach, top 100
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(
                            await run.Client.ListStringAsync(
                                $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE make = 'MERCEDES-BENZ'"),
                            test.Response.StockNos
                        );
                }
            },
            new()
            {
                Name = "Make-Filter-BMW",
                Question = "any bmws?",
                GetScoreAsync = async test =>
                {
                    // Same style, SetSimilarity
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(
                            await run.Client.ListStringAsync(
                                $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE make = 'BMW'"),
                            test.Response.StockNos
                        );
                }
            },
            new()
            {
                Name = "Bodytype-Filter-Hatchback",
                Question = "any hatchbacks?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE bodytype = 'Hatchback'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity) // was ScoreBySetSimilarity
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Bodytype-Filter-Double-Cab",
                Question = "any double cabs?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE bodytype = 'Double Cab'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Colour-Filter-White",
                Question = "any white cars?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE colour = 'White'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Colour-Filter-Black",
                Question = "any black cars?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE colour = 'Black'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNosUnique);
                }
            },
            new()
            {
                Name = "CubicCapacity-Filter-2L-Above",
                Question = "any cars with a 2+ liter engine?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no, cubiccapacity FROM {new InjectRaw(relation)} WHERE cubiccapacity >= 2000 ORDER by cubiccapacity DESC");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNosUnique);
                }
            },
            new()
            {
                Name = "CubicCapacity-Filter-2L-Below",
                Question = "any cars with an engine smaller than 2 liters?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE cubiccapacity < 2000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity) // was ScoreBySetSimilarityWeighted
                        .Score(goal, test.Response.StockNosUnique);
                }
            },
            new()
            {
                Name = "Dont-Search-Test",
                Question = "test",
                GetScore = test => test.Response.AllCarsSemantic ? 1 : 0
            },
            new()
            {
                Name = "Dont-Search-HiThere",
                Question = "hi there",
                GetScore = test => test.Response.AllCarsSemantic ? 1 : 0
            },
            new()
            {
                Name = "We-Dont-Rent",
                Question = "Any cars that I can rent?",
                GetScore = test => test.Response.AllCarsSemantic ? 1 : 0
            },
            new()
            {
                Name = "Family-Cars",
                Question = "Any family cars?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no, seats FROM {new InjectRaw(relation)} WHERE seats >= 4");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity) // was Weighted
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Cheapest-Cars",
                Question = "Show me your cheapest cars",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync($"""
                        SELECT stock_no
                        FROM {new InjectRaw(relation)}
                        ORDER BY price
                        LIMIT 100
                    """);
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Small-Jap-200-400k",
                Question = "I need a small Japanese car between 200 and R400,000 with less than 500,000 km",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync($"""
                        SELECT DISTINCT stock_no 
                        FROM {new InjectRaw(relation)} 
                        WHERE price BETWEEN 200000 AND 400000 
                          AND mileage <= 500000
                          AND make IN ('DAIHATSU', 'HONDA', 'ISUZU', 'LEXUS', 'MAZDA',
                                       'MITSUBISHI', 'NISSAN', 'SUBARU', 'SUZUKI', 'TOYOTA')
                    """);
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Old-Car",
                Question = "An old car",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync($"""
                        SELECT stock_no
                        FROM {new InjectRaw(relation)}
                        WHERE "year" <= 2014
                    """);
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "A-Newish-Vehicle",
                Question = "A newish vehicle",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync($"""
                        SELECT stock_no
                        FROM {new InjectRaw(relation)}
                        WHERE "year" >= 2020
                    """);
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "A-Bakkie",
                Question = "A bakkie",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE bodytype IN ('Single Cab', 'Double Cab')");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNosUnique);
                }
            },
            new()
            {
                Name = "A-Car-Under-60k",
                Question = "A car under 100k",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE price <= 100000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            // New Tests
            new()
            {
                Name = "Available-SUVs-Toyota-Under-500k",
                Question = "Show me available SUVs from Toyota under R500,000.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync($"""
                        SELECT DISTINCT stock_no
                        FROM {new InjectRaw(relation)}
                        WHERE make = 'TOYOTA'
                          AND bodytype = 'SUV'
                          AND price <= 500000
                    """);
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Audi-2021-Models-Less-Than-50k",
                Question = "Find all 2021 models from Audi with less than 50,000 km mileage.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE make = 'AUDI' AND \"year\" = 2021 AND mileage < 50000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Automatic-Diesel-Cars",
                Question = "List cars with automatic transmission and diesel fuel type.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE transmission = 'Automatic' AND fueltype = 'Diesel'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.SetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "White-Cars-300k-400k",
                Question = "Do you have any white cars priced between R300,000 and R400,000?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE colour = 'White' AND price BETWEEN 300000 AND 400000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Toyotas-Over-60k-Km",
                Question = "Give me all Toyotas with more than 60,000 km mileage.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE make = 'TOYOTA' AND mileage >= 60000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Manual-Petrol-Cars",
                Question = "Which cars are available with manual transmission and petrol engine?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE transmission = 'Manual' AND fueltype = 'Petrol'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Black-SUVs-Under-600k",
                Question = "Find any black SUVs with a price less than R600,000.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE bodytype = 'SUV' AND colour = 'Black' AND price <= 600000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Cars-Less-Than-20k-Under-350k",
                Question = "Are there any cars with less than 20,000 km mileage for under R350,000?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE mileage < 20000 AND price < 350000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Diesel-Cars-2020-Newer",
                Question = "List all available diesel cars from 2020 or newer.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE fueltype = 'Diesel' AND \"year\" >= 2020");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Sedans-Under-400k",
                Question = "Do you have any sedans with a price under R400,000?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE bodytype = 'Sedan' AND price < 400000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Gauteng-Automatic-Cars",
                Question = "Give me cars in Gauteng with automatic transmission.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE transmission = 'Automatic'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNosUnique);
                }
            },
            new()
            {
                Name = "White-Cars-Under-450k",
                Question = "List all cars with exterior color white and priced under R450,000.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE colour = 'White' AND price <= 450000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNosUnique);
                }
            },
            new()
            {
                Name = "SUVs-Less-Than-80k-Km",
                Question = "Find cars with a body style of SUV and mileage less than 80,000 km.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE bodytype = 'SUV' AND mileage <= 80000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Petrol-Cars-Under-300k",
                Question = "Are there any petrol cars available for under R300,000?",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE fueltype = 'Petrol' AND price <= 300000");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Cheapest-Automatic-Cars",
                Question = "Show me the cheapest automatic cars in the database.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync($"""
                        SELECT stock_no
                        FROM {new InjectRaw(relation)}
                        WHERE transmission = 'Automatic'
                        ORDER BY price ASC
                        LIMIT 100
                    """);
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Audi-Models-2021-2022",
                Question = "Find all available Audi models from 2021 and 2022.",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE make = 'AUDI' AND (\"year\" = 2021 OR \"year\" = 2022)");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Wit-Karre",
                Question = "wit karre",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE colour = 'White'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Polo",
                Question = "polo",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Model ILIKE '%Polo%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity) // was "prorataUnfound: true"
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Polo-Vivo",
                Question = "polo-vivo",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Model ILIKE '%Polo%Vivo%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Golf",
                Question = "golf",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Model ILIKE '%golf%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Tiguan",
                Question = "tiguan",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Model ILIKE '%tiguan%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Ranger",
                Question = "Ranger",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Make ILIKE 'Ford' AND Model ILIKE '%Ranger%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Ford-Ranger",
                Question = "Ford Ranger",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Make ILIKE 'Ford' AND Model ILIKE '%Ranger%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Kia Rio",
                Question = "Kia Rio",
                GetScoreAsync = async test =>
                {
                    var goal = await run.Client.ListStringAsync(
                        $"SELECT DISTINCT stock_no FROM {new InjectRaw(relation)} WHERE Make ILIKE 'Kia' AND Model ILIKE '%Rio%'");
                    return new SetEvaluatorBuilder()
                        .OnlyTop(100)
                        .UseMethod(ScoringMethod.WeightedSetSimilarity)
                        .Score(goal, test.Response.StockNos);
                }
            },
            new()
            {
                Name = "Cars-in-FreeState",
                Question = "Cars in free state",
                GetScore = test => test.Response.AllCarsSemantic ? 1 : 0
            }
        ]);


        if (filter.Length > 0)
            run.Tests.RemoveAll(t => !filter.Contains(t.Name));

        return await run.RunTestsAsync();
    }

    public async Task RunSet2Async(LuaInstance lua, string channelName = "weelee-test-1",
        string relation = "vweelee_stock_items_test_1", params string[] filter)
    {
        var channel = _sp.GetRequiredNamedService<Channel>(channelName);

        var run = new StructuredTestRunner<TestKit1Root>(
            _sp.GetRequiredNamedService<PostgreSQLClient>("pg_weelee"),
            (FlatQueryAssistant)channel.Assistant,
            lua.PrintLine);

        run.Tests.AddRange([
            new()
            {
                Name = "M3", Question = "I want an M3",
                GetScore = test => test.Response.StockNos.ScoreOrderVsUsingMRR(["SI-010059"]),
            }
        ]);

        if (filter.Length > 0)
            run.Tests.RemoveAll(t => !filter.Contains(t.Name));

        await run.RunTestsAsync();
    }

    public const string WeeleeTestKitLibrary = "WeeleeTestKit";

    public static void Register()
    {
        RegistrationService.ConfigureServices += (services, _) =>
        {
            services.AddSingleton<WeeleeTestKit>();
            services.AddKeyedSingleton<ILuaCoreLibrary>(WeeleeTestKitLibrary,
                (sp, _) => sp.GetRequiredService<WeeleeTestKit>());
        };
    }

    public async Task<double> RepeatTestAsync(LuaInstance instance, int numberOfRuns, string channelName, string relation)
    {
        var scores = new List<double>();

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < numberOfRuns; i++)
            scores.Add(await RunSet1Async(instance, channelName, relation));

        var avg = scores.Average();

        stopwatch.Stop();
        instance.PrintLine($"Done with average score of {avg:#,##0%}. Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        return avg;
    }

    public async Task RunSet3Async(LuaInstance lua, string channelName = "weelee-api-v3",
        string relation = "vweelee_stock_items_test_1")
    {
        var channel = _sp.GetRequiredNamedService<Channel>(channelName);

        var run = new StructuredTestRunner<TestKit1Root>(
            _sp.GetRequiredNamedService<PostgreSQLClient>("pg_weelee"),
            (FlatQueryAssistant)channel.Assistant,
            lua.PrintLine);

        run.Tests.AddRange([
            new()
            {
                Name = "old cars?", Question = "old cars?",
                GetScore = test => test.Response.Sort.ToKeyValuePairs().ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()).ScoreSortOrders(new() {{"year", "ASC"}})
            },
            // Test case 2: New cars
        new()
        {
            Name = "New Cars",
            Question = "Show me the latest car models.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "year", "DESC" } })
        },
        // Test case 3: Cheap cars
        new()
        {
            Name = "Cheap Cars",
            Question = "I need the most affordable cars available.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "price", "ASC" } })
        },
        // Test case 4: Expensive cars
        new()
        {
            Name = "Luxurious Cars",
            Question = "Show me the most luxurious cars you have.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>
                {
                    { "price", "DESC" },
                    { "cubiccapacity", "DESC" },
                    { "doors", "DESC" },
                    { "seats", "DESC" },
                    { "mileage", "ASC" },
                    { "year", "DESC" }
                })
        },
        // Test case 5: Low mileage cars
        new()
        {
            Name = "Low Mileage Cars",
            Question = "I'm interested in cars with the lowest mileage.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "mileage", "ASC" } })
        },
        // Test case 6: High mileage cars
        new()
        {
            Name = "High Mileage Cars",
            Question = "Show me cars with high mileage.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "mileage", "DESC" } })
        },
        // Test case 7: Cars with more doors
        new()
        {
            Name = "Cars with More Doors",
            Question = "I need cars that can seat more people, possibly with more doors.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "doors", "DESC" } })
        },
        // Test case 8: Cars with fewer doors
        new()
        {
            Name = "Cars with Fewer Doors",
            Question = "Looking for compact cars with fewer doors.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "doors", "ASC" } })
        },
        // Test case 9: Powerful cars
        new()
        {
            Name = "Powerful Cars",
            Question = "Show me cars with high engine capacity.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "cubiccapacity", "DESC" } })
        },
        // Test case 10: Fuel-efficient cars (assuming lower cubic capacity implies better fuel efficiency)
        new()
        {
            Name = "Fuel-Efficient Cars",
            Question = "I'm interested in cars that are fuel-efficient.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "cubiccapacity", "ASC" } })
        },
        // Test case 11: Cars with more seats
        new()
        {
            Name = "Cars with More Seats",
            Question = "I need a vehicle that can carry a big family.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "seats", "DESC" } })
        },
        // Test case 12: General query without specific preferences
        new()
        {
            Name = "General Query",
            Question = "Show me available cars.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>()) // Expecting all 'NONE'
        },
        // Test case 13: Old, cheap cars
        new()
        {
            Name = "Old and Cheap Cars",
            Question = "I am looking for old, cheap cars.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>
                {
                    { "year", "ASC" },
                    { "price", "ASC" }
                })
        },
        // Test case 14: New, expensive cars with high engine capacity
        new()
        {
            Name = "New, Expensive, Powerful Cars",
            Question = "I want the latest, most powerful, and luxurious cars.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>
                {
                    { "year", "DESC" },
                    { "price", "DESC" },
                    { "cubiccapacity", "DESC" }
                })
        },
        // Test case 15: Cars with fewer seats
        new()
        {
            Name = "Cars with Fewer Seats",
            Question = "Looking for small cars suitable for singles or couples.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string> { { "seats", "ASC" } })
        },
        // Test case 16: High mileage, old cars
        new()
        {
            Name = "High Mileage, Old Cars",
            Question = "Show me old cars with high mileage.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>
                {
                    { "year", "ASC" },
                    { "mileage", "DESC" }
                })
        },
        // Test case 17: Latest models with low mileage
        new()
        {
            Name = "Latest Models with Low Mileage",
            Question = "I want the newest cars with the least mileage.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>
                {
                    { "year", "DESC" },
                    { "mileage", "ASC" }
                })
        },
        // Test case 18: High seating capacity and affordable
        new()
        {
            Name = "Affordable Cars with More Seats",
            Question = "Need an affordable car that can seat at least 7 people.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>
                {
                    { "price", "ASC" },
                    { "seats", "DESC" }
                })
        },
        // Test case 19: User specifies no preference
        new()
        {
            Name = "No Preference",
            Question = "Just browsing cars.",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>()) // Expecting all 'NONE'
        },
        // Test case 20: User asks for 'any benz' (from previous interaction)
        new()
        {
            Name = "Any Benz",
            Question = "Do you have any Benz cars?",
            GetScore = test => test.Response.Sort.ToKeyValuePairs()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
                .ScoreSortOrders(new Dictionary<string, string>()) // Expecting all 'NONE'
        },
        ]);

        await run.RunTestsAsync();
    }
    
    public void Setup(LuaInstance instance)
    {
        instance.Script.Globals["testkit"] = this;
        // /test-1-api
        // /return testkit.RunSet1Async(lua, 'weelee-api', 'vweelee_stock_items')
        instance.Shortcuts["test-1-api"] = ("return testkit.RunSet1Async(lua, 'weelee-api', 'vweelee_stock_items')",
            "Run the Weelee Test 1 kit on the weelee-api channel");
        instance.Shortcuts["test-1-api-v2"] = ("return testkit.RunSet1Async(lua, 'weelee-api-v2', 'vweelee_stock_items')",
            "Run the Weelee Test 1 kit on the weelee-api channel");
        instance.Shortcuts["test-1-api-v3"] = ("return testkit.RunSet1Async(lua, 'weelee-api-v3', 'vweelee_stock_items')",
            "Run the Weelee Test 1 kit on the weelee-api channel");
        instance.Shortcuts["test-1-api-v4"] = ("return testkit.RunSet1Async(lua, 'weelee-api-v4', 'vweelee_stock_items')",
            "Run the Weelee Test 1 kit on the weelee-api channel");
        instance.Shortcuts["test-1"] = (
            "return testkit.RunSet1Async(lua, 'weelee-test-1', 'vweelee_stock_items_test_1')",
            "Run the Weelee Test 1 kit on the weelee-test-1 channel");
        instance.Shortcuts["test-2"] = (
            "return testkit.RunSet2Async(lua, 'weelee-test-1', 'vweelee_stock_items_test_1')",
            "Run the Weelee Test 2 kit on the weelee-test-1 channel");
        instance.Shortcuts["test-3"] = (
            "return testkit.RunSet3Async(lua, 'weelee-api-v3', 'vweelee_stock_items_test_1')",
            "Run Weelee Sort tests on the weelee-api channel");
        instance.Shortcuts["test-4"] = (
            "return testkit.RunSet3Async(lua, 'weelee-api-v4', 'vweelee_stock_items_test_1')",
            "Run Weelee Sort tests on the weelee-api channel");
        instance.Shortcuts["test-new"] = (
            "return testkit.RunSet1Async(lua, 'weelee-api-v3', 'vweelee_stock_items', 'Polo')",
            "Run the newest added test");
        instance.Shortcuts["repeat-test"] = ("return testkit.RepeatTestAsync(lua, 5, 'weelee-api-v3', 'vweelee_stock_items')",
            "Run the standard Weelee load test");
    }
}