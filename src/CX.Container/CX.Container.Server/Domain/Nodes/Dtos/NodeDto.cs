using System.ComponentModel.DataAnnotations;

namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node.
/// </summary>
public sealed record NodeDto
{
    /// <summary>
    /// Unique Identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique Identifier linking to this Node's <see cref="Projects.Project"/>.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Unique Identifier linking to this Node's Parent Node.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Unique Identifier linking to this Node's Source.
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Name of the Node.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the Node.
    /// </summary>
    [MaxLength(254)]
    public string Description { get; set; }

    /// <summary>
    /// Author of the Node's file.
    /// </summary>    
    public string Author { get; set; }

    /// <summary>
    /// Language of the Node's file.
    /// </summary>    
    public string Language { get; set; }

    /// <summary>
    /// Indicates if this is an Asset and not a Category Node.
    /// </summary>
    public bool IsAsset { get; set; }

    //// <summary>
    //// Extension of the Node's file.
    //// </summary>
    //public string FileExt { get; set; }

    /// <summary>
    /// Date of publication of the Node's file.
    /// </summary>
    public string Publication { get; set; }

    /// <summary>
    /// URI of the Node if it is a url.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// S3Key of the Node if it is a file.
    /// </summary>
    public string S3Key { get; set; }

    /// <summary>
    /// Name of the file of the Node.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Display name of the file of the Node.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Score out of 10 for relevance to agriculture.
    /// </summary>
    public int AgriRelevance { get; set; }

    /// <summary>
    /// Keywords of the Node.
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// Tags of the Node.
    /// </summary>
    public string Tags { get; set; }
}