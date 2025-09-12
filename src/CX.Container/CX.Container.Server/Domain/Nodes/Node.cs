namespace CX.Container.Server.Domain.Nodes;

using System.ComponentModel.DataAnnotations;
using CX.Container.Server.Domain.Projects;
using CX.Container.Server.Domain.Nodes.Models;
using CX.Container.Server.Domain.Nodes.DomainEvents;
using Microsoft.IdentityModel.Tokens;
using CX.Container.Server.Domain.Sources;

public class Node : Entity<Guid>
{
    [Required]
    [MaxLength(100)]
    public string Name { get; private set; }

    
    [MaxLength(200)]
    public string FileName { get; private set; }

    
    [MaxLength(200)]
    public string DisplayName { get; private set; }

    
    public string Description { get; private set; }

    
    [MaxLength(50)]
    public string Author { get; private set; }

    
    [MaxLength(85)]
    public string Language { get; private set; }

    public bool IsAsset { get; private set; }

    
    [MaxLength(4)]
    public string FileExt { get; private set; }

    
    public string Url { get; private set; }

    
    public string S3Key { get; private set; }

    
    [MaxLength(200)]
    public string Keywords { get; private set; }

    
    [MaxLength(200)]
    public string Tags { get; private set; }

    
    [MaxLength(10)]
    public string Publication { get; private set; }

    
    public int? AgriRelevance { get; private set; }

    public Guid? SourceId { get; private set; }
    public Source Source { get; private set; }


    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; }


    public Guid? ParentId { get; private set; }
    public virtual Node Parent { get; private set; }


    public virtual IReadOnlyCollection<Node> Nodes { get; } = new List<Node>();


    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Node Create(NodeForCreation nodeForCreation)
    {
        var newNode = new Node
        {
            SourceId = nodeForCreation.SourceId,
            ProjectId = nodeForCreation.ProjectId,
            ParentId = nodeForCreation.ParentId,
            Name = nodeForCreation.Name,
            Description = nodeForCreation.Description,
            IsAsset = nodeForCreation.IsAsset,
            Keywords = nodeForCreation.Keywords,
            Tags = nodeForCreation.Tags
        };

        newNode.QueueDomainEvent(new NodeCreated(){ Node = newNode });

        return newNode;
    }

    public Node Update(NodeForUpdate nodeForUpdate)
    {
        if (nodeForUpdate.SourceId.HasValue) SourceId = nodeForUpdate.SourceId;

        if (!nodeForUpdate.Name.IsNullOrEmpty()) Name = nodeForUpdate.Name;

        Description = nodeForUpdate.Description;

        Language = nodeForUpdate.Language;

        Keywords = nodeForUpdate.Keywords;

        Tags = nodeForUpdate.Tags;

        Author = nodeForUpdate.Author;

        AgriRelevance = nodeForUpdate.AgriRelevance;

        Publication = nodeForUpdate.Publication;

        QueueDomainEvent(new NodeUpdated(){ Id = Id });
        return this;
    }

    public Node SetProject(Project project)
    {
        Project = project;
        return this;
    }

    public Node SetParentNode(Node node)
    {
        Parent = node;
        return this;
    }

    public Node SetS3Key(string s3Key, string fileName, string displayName)
    {
        S3Key = s3Key;
        FileName = fileName;
        DisplayName = displayName;
        return this;
    }

    public Node ClearFileMetaData()
    {
        S3Key = null;
        FileName = null;
        DisplayName = null;
        Language = null;
        Keywords = null;
        Tags = null;
        Author = null;
        AgriRelevance = null;
        SourceId = null;
        Publication = null;
        Description = null;
        return this;
    }

    public Node SetIsDeleted()
    {
        IsDeleted = true;
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete

    protected Node() { } // For EF + Mocking
}
