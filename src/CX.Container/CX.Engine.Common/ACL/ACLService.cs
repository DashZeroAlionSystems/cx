using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CX.Engine.Common.ACL;

public class ACLService : IDisposable
{
    private readonly IDisposable _optionsMonitorDisposable;

    public ACLServiceOptions Options;

    public ACLService(IOptionsMonitor<ACLServiceOptions> options, ILogger<ACLService> logger, IServiceProvider sp)
    {
        _optionsMonitorDisposable = options.Snapshot(() => Options, v => Options = v, logger, sp);
    }

    public bool IsAllowed(string apiKey, string permission)
    {
        if (!Options.APIKeys.TryGetValue(apiKey, out var keyEntry))
            return false;

        if (keyEntry.Deny != null)
            foreach (var deny in keyEntry.Deny)
            {
                if (Regex.IsMatch(permission, deny))
                    return false;
            }

        if (keyEntry.Allow != null)
            foreach (var allow in keyEntry.Allow)
            {
                if (Regex.IsMatch(permission, allow))
                    return true;
            }

        return false;
    }

    public void Dispose()
    {
        _optionsMonitorDisposable?.Dispose();
    }
}