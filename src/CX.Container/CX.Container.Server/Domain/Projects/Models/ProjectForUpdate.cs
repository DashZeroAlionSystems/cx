namespace CX.Container.Server.Domain.Projects.Models;
public sealed class ProjectForUpdate
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Thumbnail { get; set; }

    public string Namespace { get; set; }

}
