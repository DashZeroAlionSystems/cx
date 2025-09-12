namespace CX.Container.Server.Domain.UiLogs.Dtos;

using Destructurama.Attributed;

public sealed record UiLogsDto
{
    public Guid Id { get; set; }
    public string From { get; set; }
    public string Details { get; set; }

}
