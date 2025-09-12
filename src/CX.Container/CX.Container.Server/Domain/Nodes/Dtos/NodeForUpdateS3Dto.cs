namespace CX.Container.Server.Domain.Nodes.Dtos;

/// <summary>
/// Data Transfer Object representing a Node's S3 Key to be Updated.
/// </summary>
public sealed record NodeForUpdateS3Dto
{
    /// <summary>
    /// S3Key of the Node if it is a file.
    /// </summary>
    public string S3Key { get; set; }

    /// <summary>
    /// File Name of the Node if it is a file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Display Name of the Node if it is a file.
    /// </summary>
    public string DisplayName { get; set; }

}