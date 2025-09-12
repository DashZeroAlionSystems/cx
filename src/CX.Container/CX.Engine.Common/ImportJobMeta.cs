using System.Text.Json;
using JetBrains.Annotations;

namespace CX.Engine.Common;

/// <summary>
/// File meta in a System.Text.Json friendly format.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class ImportJobMeta
{
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string FileDescription { get; set; }
    public string DirectoryPath { get; set; }
    public string DirectoryPattern { get; set; }
    public string FilePath { get; set; } 
    public string Archive { get; set; }
    public AttachmentInfo[] Attachments { get; set; }
    public JsonDocument Info { get; set; }

    public string SourceDocument { get; set; }
    public string Organization { get; set; }

    public bool IsFileBased;
    public bool IsAttachment { get; set; }
    public bool Skip { get; set; }
    public bool? ExtractImages { get; set; }
    public bool? PreferVisionTextExtractor { get; set; }

    public void Validate(string contentDirectory)
    {
        if (string.IsNullOrWhiteSpace(FilePath) && string.IsNullOrWhiteSpace(DirectoryPath))
            throw new InvalidOperationException("FilePath or DirectoryPath}.");

        if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(DirectoryPath))
            throw new InvalidOperationException("FilePath and DirectoryPath cannot both be specified.");
        
        IsFileBased = !string.IsNullOrWhiteSpace(FilePath);

        if (IsFileBased)
        {
            var filePath = FilePath?.Replace("{Content}", contentDirectory);

            if (!Skip && !File.Exists(filePath))
                throw new InvalidOperationException($"File {FilePath} does not exist (resolved to {filePath}).");
        }
        else
        {
            var directoryPath = DirectoryPath?.Replace("{Content}", contentDirectory);
            if (!Skip && !Directory.Exists(directoryPath))
                throw new InvalidOperationException($"Directory {DirectoryPath} does not exist (resolved to {directoryPath}).");

            if (string.IsNullOrWhiteSpace(DirectoryPattern))
                throw new InvalidOperationException($"DirectoryPattern is required for directory {DirectoryPath}");
        }
    }

    public void Overwrite(ImportJobMeta overwriteWith)
    {
        if (overwriteWith == null)
            return;

        if (overwriteWith.Organization != null)
            Organization = Organization;

        if (overwriteWith.SourceDocument != null)
            SourceDocument = SourceDocument;

        if (overwriteWith.FileId != null)
            FileId = overwriteWith.FileId;

        if (overwriteWith.FileDescription != null)
            FileDescription = overwriteWith.FileDescription;

        if (overwriteWith.FileName != null)
            FileName = overwriteWith.FileName;

        if (overwriteWith.Info != null)
            Info = overwriteWith.Info;

        if (overwriteWith.Attachments != null)
        {
            if (Attachments == null)
                Attachments = overwriteWith.Attachments;
            else
                Attachments = Attachments.Concat(overwriteWith.Attachments).Distinct().ToArray();
        }
        
        IsAttachment |= overwriteWith.IsAttachment;
    }

    public ImportJobMeta Clone()
    {
        var res = new ImportJobMeta();
        res.SourceDocument = SourceDocument;
        res.Organization = Organization;
        res.DirectoryPath = DirectoryPath;
        res.Skip = Skip;
        res.DirectoryPattern = DirectoryPattern;
        res.FilePath = FilePath;
        res.IsFileBased = IsFileBased;
        res.FileId = FileId;
        res.FileName = FileName;
        res.FileDescription = FileDescription;
        res.ExtractImages = ExtractImages;
        res.PreferVisionTextExtractor = PreferVisionTextExtractor;
        res.Info = Info;

        if (Attachments != null)
            res.Attachments = Attachments;

        return res;
    }
}