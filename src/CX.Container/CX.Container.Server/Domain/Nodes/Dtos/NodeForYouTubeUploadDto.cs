namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node Asset.
/// </summary>
public sealed record NodeForYouTubeUploadDto
{
    /// <summary>
    /// Unique identifier specifying this Asset's <see cref="Nodes.Node"/>
    /// </summary>    
    public Guid NodeId { get; set; }

    /// <summary>
    /// YouTube URL.
    /// </summary>    
    public string Url { get; set; }
    
    /// <summary>
    /// Language to transcribe to
    /// </summary>
    public string Language { get; set; }
}