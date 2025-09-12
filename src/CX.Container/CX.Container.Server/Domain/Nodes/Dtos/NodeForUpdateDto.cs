using System.ComponentModel.DataAnnotations;

namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node to be Updated.
/// </summary>
public sealed record NodeForUpdateDto
{
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
    /// Year of publication of the Node's file.
    /// </summary>
    public string Publication { get; set; }

    /// <summary>
    /// Agriculture relevance score of the Node's file.
    /// </summary>
    public int? AgriRelevance { get; set; }

    /// <summary>
    /// Indicates if this is an Asset and not a Category Node.
    /// </summary>
    [Required]
    public bool IsAsset { get; set; }

    /// <summary>
    /// Keywords of the Node.
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// Tags of the Node.
    /// </summary>
    public string Tags { get; set; }
}