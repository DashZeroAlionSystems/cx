namespace CX.Container.Server.Domain.Users.Dtos;

public class CreatedUserSummaryDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
}