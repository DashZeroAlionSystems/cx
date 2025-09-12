namespace CX.Container.Server.Domain.UiLogs.Models;

using Destructurama.Attributed;

public sealed record UiLogsForCreation
{
    public string From { get; set; }
    public string Details { get; set; }
}
