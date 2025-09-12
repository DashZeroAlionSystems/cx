namespace CX.Container.Server.Domain.Nodes.Models;

public sealed record NodeForUpdate
{    
    public Guid? SourceId { get; set; }
    public string Name { get; set; }    
    public string Description { get; set; }
    public string Author { get; set; }
    public string Language { get; set; }
    public string Keywords { get; set; }
    public string Tags { get; set; }
    public string Publication { get; set; }
    public int AgriRelevance { get; set; }
}