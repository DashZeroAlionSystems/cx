namespace CX.Container.Server.Domain.UiLogs.Dtos;

using Destructurama.Attributed;

public sealed record UiLogsForUpdateDto
{
    public string From { get; set; }
    public string Details { get; set; }
}
