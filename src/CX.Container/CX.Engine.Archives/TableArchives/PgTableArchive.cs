using CX.Engine.Common.PostgreSQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Archives.TableArchives;

public class PgTableArchive : IArchive, ISnapshottedOptions<PgTableArchive.Snapshot, PgTableArchiveOptions, PgTableArchive>
{
    public readonly string Name;
    private readonly ILogger _logger;
    private readonly IServiceProvider _sp;
    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<PgTableArchiveOptions> OptionsSection { get; set; }

    [UsedImplicitly]
    public class Snapshot : Snapshot<PgTableArchiveOptions, PgTableArchive>, ISnapshotSyncInit<PgTableArchiveOptions>
    {
        public PostgreSQLClient Sql;

        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
            sp.GetRequiredNamedService(out Sql, Options.PostgreSQLClientName, section);
        }
    }


    public PgTableArchive(string name, MonitoredOptionsSection<PgTableArchiveOptions> optionsSection, [NotNull] ILogger logger, [NotNull] IServiceProvider sp)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        optionsSection.Bind<Snapshot, PgTableArchive>(this);
    }

    public Task ClearAsync()
    {
        throw new NotImplementedException();
    }

    public Task RemoveDocumentAsync(Guid documentId)
    {
        throw new NotImplementedException();
    }
}