using CX.Engine.Assistants.AssessmentBuilder;
using CX.Engine.Assistants.CachedAssistants;
using CX.Engine.Assistants.FlatQuery;
using CX.Engine.Assistants.LuaAssistants;
using CX.Engine.Assistants.MenuAssistants;
using CX.Engine.Assistants.Options;
using CX.Engine.Assistants.PgTableEnrichment;
using CX.Engine.Assistants.QueryAssistants;
using CX.Engine.Assistants.TextToSchema;
using CX.Engine.Assistants.VectorMind;
using CX.Engine.Common;
using CX.Engine.Assistants.Walter1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Assistants;

public static class AssistantDI
{
    public const string VectormindLiveEngineName = "vectormind-live";
    public const string Walter1EngineName = "walter-1";
    public const string FlatQueryEngineName = "flat-query";
    public const string SqlServerQueryEngineName = "sql-server-query"; 
    public const string SqlServerReportEngineName = "sql-server-report"; 
    public const string TextToSchemaEngineName = "text-to-schema";
    public const string LuaEngineName = "lua";
    public const string MenuEngineName = "menu";
    public const string Crc32CachedEngineName = "crc32";
    public const string SOSPrototypeEngineName = "sos-prototype";
    public const string PgTableEnrichmentEngineName = "pg-table-enrichment";
    public const string AssessmentBuildeEngineName = "assessment-builder";
    
    public static void AddAssistants(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddVectormindLiveAssistants(configuration);
        sc.AddWalter1Assistants(configuration);
        sc.AddTextToSchemaAssistants(configuration);
        sc.AddFlatQueryAssistants(configuration);
        sc.AddSqlServerQueryAssistants(configuration);
        sc.AddSqlServerReportAssistants(configuration);
        sc.AddLuaAssistants(configuration);
        sc.AddOptionsAssistants(configuration);
        sc.AddMenuAssistants(configuration);
        sc.AddCrc32CachedAssistants(configuration);
        sc.AddAssessmentBuilderAssistants(configuration);

        var router = sc.AddNamedTransientRouter<IAssistant>(configuration, "assistant engine");
        router[VectormindLiveEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<VectormindLiveAssistant>(subName, optional);
        router[Walter1EngineName] = static (subName, sp, _, optional) => sp.GetNamedService<Walter1Assistant>(subName, optional);
        router[FlatQueryEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<FlatQueryAssistant>(subName, optional);
        router[TextToSchemaEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<TextToSchemaAssistant>(subName, optional);
        router[LuaEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<LuaAssistant>(subName, optional);
        router[SqlServerQueryEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<SqlServerQueryAssistant>(subName, optional);
        router[SqlServerReportEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<SqlServerReportAssistant>(subName, optional);
        router[MenuEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<MenuAssistant>(subName, optional);
        router[Crc32CachedEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<Crc32CachedAssistant>(subName, optional);
        router[PgTableEnrichmentEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<PgTableEnrichmentAssistant>(subName, optional);
        router[AssessmentBuildeEngineName] = static (subName, sp, _, optional) => sp.GetNamedService<AssessmentAssistant>(subName, optional);
    }
}