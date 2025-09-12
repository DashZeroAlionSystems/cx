namespace CX.Container.Server.Domain.UiLogs.Dtos;

using Destructurama.Attributed;

public sealed record UiLogsForCreationDto
{
    public string From { get; set; }
    public string Details { get; set; }
}
