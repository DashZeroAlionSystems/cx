using CX.Engine.Archives.InMemory;
using CX.Engine.Archives.PgVector;
using CX.Engine.Archives.TableArchives;

namespace CX.Engine.Archives;

public static class ArchiveRouter
{
    public const string InMemory = "in-memory";
    public const string PineconeReadOnly = "pinecone-readonly";
    public const string Pinecone = "pinecone";
    public const string PineconeNamespace = "pinecone-namespace";
    public const string PgVector = "pg-vector";
    public const string PgTable = "pg-table";

    public static void AddArchives(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddInMemoryArchives(configuration);
        sc.AddPinecone1Archives(configuration);
        sc.AddPgVectorArchives(configuration);
        sc.AddPgTableArchives(configuration);
        sc.AddArchiveRouters(configuration);
    }

    private static void AddArchiveRouters(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddNamedSingletons<IArchive>(configuration,
            (sp, _, name, optional) =>
            {
                var (engine, subName) = name.SplitAtFirst(".");

                return engine switch
                {
                    InMemory => sp.GetNamedService<InMemoryChunkArchive>(subName, optional),
                    PineconeReadOnly => sp.GetNamedService<PineconeReadOnlyChunkArchive>(subName, optional),
                    PineconeNamespace => sp.GetNamedService<PineconeNamespace>(subName, optional),
                    Pinecone => sp.GetNamedService<PineconeChunkArchive>(subName, optional),
                    PgVector => sp.GetNamedService<PgVectorChunkArchive>(subName, optional),
                    PgTable => sp.GetNamedService<PgTableArchive>(subName, optional),
                    _ => throw new InvalidOperationException($"Unknown archive engine '{engine}'.")
                };
            });

        sc.AddNamedSingletons<IChunkArchive>(configuration,
            (sp, _, name, optional) =>
            {
                var (engine, subName) = name.SplitAtFirst(".");

                return engine switch
                {
                    InMemory => sp.GetNamedService<InMemoryChunkArchive>(subName, optional),
                    PineconeReadOnly => sp.GetNamedService<PineconeReadOnlyChunkArchive>(subName, optional),
                    PineconeNamespace => sp.GetNamedService<PineconeNamespace>(subName, optional),
                    Pinecone => sp.GetNamedService<PineconeChunkArchive>(subName, optional),
                    PgVector => sp.GetNamedService<PgVectorChunkArchive>(subName, optional),
                    //PgTable => sp.GetNamedService<PgTableArchive>(subName, optional),
                    _ => throw new InvalidOperationException($"Unknown chunk archive engine '{engine}'.")
                };
            });
    }
}