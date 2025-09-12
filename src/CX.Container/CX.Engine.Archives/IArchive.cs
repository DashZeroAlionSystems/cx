namespace CX.Engine.Archives;

public interface IArchive
{
    Task ClearAsync();
    Task RemoveDocumentAsync(Guid documentId);
}