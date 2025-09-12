namespace CX.Container.Server.Domain.Nodes.Models;

public sealed record NodeForCreation
{
    public Guid? SourceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }    
    public string Name { get; set; }    
    public string Description { get; set; }
    public bool IsAsset { get; set; }    
    public string Keywords { get; set; }
    public string Tags { get; set; }
}