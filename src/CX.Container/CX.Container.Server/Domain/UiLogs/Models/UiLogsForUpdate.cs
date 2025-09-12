namespace CX.Container.Server.Domain.UiLogs.Models;

using Destructurama.Attributed;

public sealed record UiLogsForUpdate
{
    public string From { get; set; }
    public string Details { get; set; }
}
