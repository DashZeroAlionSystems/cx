using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common;

public interface ISnapshotSyncInit<in TOptions>
{
    void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp);
}