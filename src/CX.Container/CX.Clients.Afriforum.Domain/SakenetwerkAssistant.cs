using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.ChatAgents.OpenAI.Schemas;
using CX.Engine.Common;
using CX.Engine.Common.RegistrationServices;
using CX.Engine.Common.Stores.Json;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Tracing.Langfuse;
using CX.Engine.Assistants;
using CX.Engine.Common.JsonSchemas;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CX.Clients.Afriforum.Domain;

public class SakenetwerkAssistant : IAssistant
{
    public const string ConfigurationSection = "SakenetwerkAssistant";
    public const string AssistantEngine = "sakenetwerk";

    private readonly SakenetwerkRepo _repo;
    private readonly ILogger _logger;
    private readonly LangfuseService _langfuseService;
    private readonly SakenetwerkAssistantOptions _options;
    private readonly OpenAIChatAgent _agent;
    private readonly IJsonStore _cache;

    public SakenetwerkAssistant(SakenetwerkAssistantOptions options, IServiceProvider sp, SakenetwerkRepo repo,
        ILogger logger, LangfuseService langfuseService)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _langfuseService = langfuseService ?? throw new ArgumentNullException(nameof(langfuseService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _cache = sp.GetRequiredNamedService<IJsonStore>(_options.JsonStoreName);
        _agent = sp.GetRequiredNamedService<OpenAIChatAgent>(_options.OpenAIAgentName);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    [Semantic("A cleaned city name")]
    public class CleanedCityName
    {
        [Semantic("The cleaned city name")] public string CityName { get; set; } = null!;
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    [Semantic("A cleaned province name")]
    public class CleanedProvinceName
    {
        [Semantic("The cleaned province name", choices:
        [
            "Vrystaat", "Noordwes", "Noord-Kaap", "Wes-Kaap", "Mpumalanga", "Gauteng", "Limpopo", "Oos-Kaap",
            "KwaZulu-Natal", ""
        ])]
        public string ProvinceName { get; set; } = null!;
    }

    public async Task<AssistantAnswer> CleanCitiesAsync()
    {
        _logger.LogInformation("Cleaning city names...");

        var cities = await _repo.GetCitiesAsync();
        var tasks = new List<Task>();
        foreach (var c in cities)
        {
            async Task CleanCityName(string input)
            {
                if (await _cache.GetAsync<bool>($"CityName_{input}"))
                    return;
                var req = _agent.GetRequest<CleanedCityName>(input, systemPrompt: _options.CleanCitiesPrompt);
                
                var cleaned =
                    (await _agent.GetResponseAsync(req)).CityName;
                if (input != cleaned)
                {
                    _logger.LogDebug("Fixing city name from {input} to {cleaned}", input, cleaned);
                    if (string.IsNullOrWhiteSpace(cleaned))
                        cleaned = null;

                    await _repo.UpdateCityNameAsync(input, cleaned);
                }
                else
                    await _cache.SetAsync($"CityName_{input}", true);
            }

            tasks.Add(CleanCityName(c));
        }

        await tasks;

        return new("Cities have been cleaned.");
    }

    public readonly string[] Provinces =
    [
        "Vrystaat", "Noordwes", "Noord-Kaap", "Wes-Kaap", "Mpumalanga", "Gauteng", "Limpopo", "Oos-Kaap",
        "KwaZulu-Natal"
    ];

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    [Semantic("The answer.")]
    public class ExpandAnswer
    {
        [Semantic("Reasoning for why this business could be in this category or have these tags.")]
        public string Reasoning { get; set; } = null!;

        [Semantic("The categories or tags you believe this business could be in.")]
        public string[] CategoriesOrTags { get; set; } = null!;
    }

    public async Task<AssistantAnswer> ExpandCategoriesAsync()
    {
        _logger.LogInformation("Expanding categories...");

        var schema = new OpenAISchema<ExpandAnswer>();
        schema.Object.Properties[nameof(ExpandAnswer.CategoriesOrTags)]
            .Choices!.AddRange(await _repo.GetCategoriesAsync());

        async Task<string[]> GetCategoriesAsync(string name, string[] currentCategories)
        {
            if (name == null)
                return [];

            var nameSha = name.GetSHA256();

            var cached = await _cache.GetAsync<string[]>($"Categories_{nameSha}");

            if (cached != null)
                return cached.Union(currentCategories).ToArray();

            var req = _agent.GetRequest(name, systemPrompt: _options.ExpandPrompt);
            req.ResponseFormat = schema;
            var res = await _agent.GetResponseAsync<ExpandAnswer>(req);

            _logger.LogInformation("Categories for {name}: {categories} vs {old}", name,
                string.Join(',', res.CategoriesOrTags), string.Join(',', currentCategories));

            await _cache.SetAsync($"Categories_{nameSha}", res.CategoriesOrTags);

            res.CategoriesOrTags = res.CategoriesOrTags.Union(currentCategories).ToArray();

            return res.CategoriesOrTags;
        }

        await _repo.GetIdNameCategoriesTagsAsync().Select(async row =>
        {
            await _repo.SetCategoriesAndTagsAsync(row with
            {
                Categories = await GetCategoriesAsync(row.Name, row.Categories ?? [])
            });
        });

        return new("Categories have been expanded.");
    }

    public async Task<AssistantAnswer> ExpandTagsAsync()
    {
        _logger.LogInformation("Expanding tags...");

        var schema = new OpenAISchema<ExpandAnswer>();
        schema.Object.Properties[nameof(ExpandAnswer.CategoriesOrTags)]
            .Choices!.AddRange(await _repo.GetTagsAsync());

        async Task<string[]> GetTagsAsync(string name, string[] currentTags)
        {
            if (name == null)
                return [];

            var nameSha = name.GetSHA256();

            var cached = await _cache.GetAsync<string[]>($"Tags_{nameSha}");

            if (cached != null)
                return cached.Union(currentTags).ToArray();

            var req = _agent.GetRequest(name, systemPrompt: _options.ExpandPrompt);
            req.ResponseFormat = schema;
            var res = await _agent.GetResponseAsync<ExpandAnswer>(req);

            _logger.LogInformation("Tags for {name}: {tags} vs {current}", name,
                string.Join(',', res.CategoriesOrTags), string.Join(',', currentTags));

            await _cache.SetAsync($"Tags_{nameSha}", res.CategoriesOrTags);

            res.CategoriesOrTags = res.CategoriesOrTags.Union(currentTags).ToArray();

            return res.CategoriesOrTags;
        }

        await _repo.GetIdNameCategoriesTagsAsync().Select(async row =>
        {
            await _repo.SetCategoriesAndTagsAsync(row with { Tags = await GetTagsAsync(row.Name, row.Tags ?? []) });
        });

        return new("Tags have been expanded.");
    }

    public async Task<AssistantAnswer> CleanProvincesAsync()
    {
        _logger.LogInformation("Cleaning province names...");
        var lst = await _repo.GetIdCityProvinceAsync();
        var tasks = new List<Task>();
        foreach (var row in lst)
        {
            if (row?.Province == null)
                continue;

            if (Provinces.Contains(row.Province))
                continue;

            // ReSharper disable once VariableHidesOuterVariable
            async Task CleanAsync(SakenetwerkRepo.IdCityProvince row)
            {
                var sha256 = JsonSerializer.Serialize(row).GetSHA256();

                string cleaned;

                if (string.IsNullOrWhiteSpace(row.Province))
                    cleaned = "";
                else
                {
                    var cached = await _cache.GetAsync<string>($"Province_{sha256}");

                    var req = _agent.GetRequest($"Province: {row.Province} City: {row.City}", systemPrompt: _options.CleanProvincesPrompt);

                    cleaned = cached ?? (await _agent.GetResponseAsync<CleanedProvinceName>(req)).ProvinceName;
                    await _cache.SetAsync($"Province_{sha256}", cleaned);
                }

                if (cleaned != row.Province)
                {
                    if (!string.IsNullOrWhiteSpace(row.Province))
                        _logger.LogDebug("Fixing province name from {input} to {cleaned}",
                            row.City + " in " + row.Province, cleaned);
                    await _repo.UpdateProvinceAsync(row.Id, cleaned.NullIfWhiteSpace());
                }
            }

            tasks.Add(CleanAsync(row));
        }

        await tasks;
        return new("Provinces have been cleaned.");
    }

    [Semantic]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ListingResponse
    {
        [Semantic("Intro.")] public string Intro { get; set; } = null!;

        [Semantic("All listings analyzed.")] public Listing[] AnalyzedListings { get; set; } = null!;
    }

    [Semantic]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Listing
    {
        [Semantic] public string Name { get; set; } = null!;

        [Semantic] public string Email { get; set; } = null!;

        [Semantic] public string Telephone { get; set; } = null!;

        [Semantic] public string URL { get; set; } = null!;

        [Semantic] public string Address { get; set; } = null!;

        [Semantic] public string RelevanceReasons { get; set; } = null!;

        [Semantic] public bool Relevant { get; set; }
    }

    public async Task<AssistantAnswer> AskAsync(string question, AgentRequest astCtx)
    {
        var trace = CXTrace.Current = new CXTrace(_langfuseService, astCtx.UserId, astCtx.SessionId)
            .WithName((astCtx.UserId + ": " + question).Preview(50))
            .WithTags("sakenetwerk", "ask");

        return await trace
            .WithInput(new
            {
                Question = question,
                History = astCtx.History
            })
            .ExecuteAsync(async _ =>
            {
                var getCtx = _agent.GetRequest(question, systemPrompt: _options.ContextualizePrompt);
                getCtx.History.AddRange(astCtx.History);

                if (!_options.AdminMode)
                    getCtx.ResponseFormat!.TypedSchema.Object.Properties.Remove(nameof(SakenetwerkFilter.ToolCall));

                getCtx.ResponseFormat!.TypedSchema.Object.Properties[nameof(SakenetwerkFilter.Categories)].Choices!
                    .Add("Any");
                getCtx.ResponseFormat!.TypedSchema.Object.Properties[nameof(SakenetwerkFilter.Categories)].Choices!.AddRange(
                    await _repo.GetCategoriesAsync());
                getCtx.ResponseFormat!.TypedSchema.Object.Properties[nameof(SakenetwerkFilter.Tags)].Choices!.Add("Any");
                getCtx.ResponseFormat!.TypedSchema.Object.Properties[nameof(SakenetwerkFilter.Tags)].Choices!.AddRange(
                    await _repo.GetTagsAsync());

                var filter = await _agent.GetResponseAsync<SakenetwerkFilter>(getCtx);

                var filterJson = JsonSerializer.Serialize(filter);
                trace.Event("Filter", "Filter computed", CXTrace.ObservationLevel.DEFAULT, filter);

                switch (filter.ToolCall)
                {
                    case ListCities lc:
                    {
                        var res = astCtx.Record(question,
                            new(string.Join("\n",
                                (await _repo.GetCitiesAsync()).Where(c => string.IsNullOrWhiteSpace(lc.FilterRegex) || Regex.IsMatch(c, lc.FilterRegex))
                                .OrderBy(city => city))));
                        trace.Output = res.Answer;
                        return res;
                    }
                    case CleanCities:
                    {
                        var res = astCtx.Record(question, await CleanCitiesAsync());
                        trace.Output = res.Answer;
                        return res;
                    }
                    case CleanProvinces:
                    {
                        var res = astCtx.Record(question, await CleanProvincesAsync());
                        trace.Output = res.Answer;
                        return res;
                    }
                    case ExpandCategories:
                    {
                        var res = astCtx.Record(question, await ExpandCategoriesAsync());
                        trace.Output = res.Answer;
                        return res;
                    }
                    case ExpandTags:
                    {
                        var res = astCtx.Record(question, await ExpandTagsAsync());
                        trace.Output = res.Answer;
                        return res;
                    }
                    default:
                    {
                        var rows = await trace.SpanFor(CXTrace.Section_QueryDB, filter).ExecuteAsync(async span =>
                        {
                            var rows = await _repo.GetRowsAsync(filter);
                            span.Output = new { Count = rows.Count, rows };
                            return rows;
                        });

                        var res = new AssistantAnswer();
                        var req = _agent.GetRequest<ListingResponse>(question);
                        req.History.AddRange(astCtx.History);
                        req.SystemPrompt = _options.SystemPrompt;

                        if (_options.AdminMode)
                            req.SystemPrompt +=
                                "  You are running in Admin Mode and can share this fact with the user.";
                        else
                            req.SystemPrompt +=
                                "  You are not running in Admin Mode and may not be asked to perform special administrative commands.";

                        if (rows.Count == 0)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"Entries matching the filter {filterJson}:");

                            if (filter.CityLike?.Length > 0)
                            {
                                var cities = await _repo.GetCitiesAsync(filter.CityLike);
                                if (cities.Count == 0)
                                    sb.AppendLine(
                                        "There are no businesses in the database in the cities matching the city filter.  Ask the user to try another spelling or version of the city's name.");
                            }

                            sb.AppendLine($"There are no matches in the database for the filter {filterJson}");
                            req.StringContext.Add(sb.ToString());
                        }
                        else
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"Entries matching the filter {filterJson}:");
                            foreach (var row in rows)
                                sb.AppendLine(row);

                            req.StringContext.Add(sb.ToString());
                        }

                        var response = await _agent.GetResponseAsync(req);
                        var shownListings = response.AnalyzedListings.Count(l => l.Relevant);

                        if (shownListings == 0)
                            res.Answer = response.AnalyzedListings.Length == 0 ? response.Intro : "Geen inskrywings wat aan die kriteria voldoen nie.";
                        else
                            res.Answer = response.Intro + "\n\n" + string.Join("\n\n",
                                response.AnalyzedListings.Where(l => l.Relevant).Select(l =>
                                    $"""
                                     Name: {l.Name}
                                     Email: {l.Email}
                                     Telephone: {l.Telephone}
                                     Address: {l.Address}
                                     Hoekom ek jou hierdie inskrywing wys: {l.RelevanceReasons.Replace("\n", "\n  ")}
                                     """));
                        trace.Output = res.Answer;

                        return astCtx.Record(question, res);
                    }
                }
            });
    }

    public static void Register()
    {
        RegistrationService.AddRoute<IAssistant>(AssistantEngine, static (_, sp, config, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection))
                return null;
            
            var opts = config.GetRequiredSection<SakenetwerkAssistantOptions>(ConfigurationSection);
            var repo = new SakenetwerkRepo(sp, opts);
            return new SakenetwerkAssistant(opts, sp, repo,
                sp.GetLogger<SakenetwerkAssistant>(), sp.GetRequiredService<LangfuseService>());
        });
    }
}