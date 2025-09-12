using CX.Engine.Common.Storage;

namespace CX.Engine.Common
{
    /// <summary>
    ///   Defines storage operations for persisting and retrieving named content blobs
    ///   (files, text, etc.) and obtaining direct URLs or streamed responses.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        ///   Retrieves the stored content (data stream, metadata, etc.) for the document
        ///   identified by the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the document to fetch.
        /// </param>
        /// <returns>
        ///   A <see cref="StorageResponseBase"/> containing the data stream, content type,
        ///   file name, and any other metadata needed to consume or download the file.
        /// </returns>
        Task<StorageResponseBase> GetContentAsync(string id);

        /// <summary>
        ///   Generates or retrieves a publicly-accessible URL for the document
        ///   identified by <paramref name="id"/>, allowing clients to download it directly.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the document.
        /// </param>
        /// <returns>

        ///   A fully-qualified URL string pointing to the stored document.
        /// </returns>
        Task<string> GetContentUrlAsync(string id);

        /// <summary>
        ///   Inserts a new document or updates an existing one under the given <paramref name="name"/>.
        ///   The <paramref name="content"/> is stored as UTF-8 text.
        /// </summary>
        /// <param name="name">
        ///     The key or filename to assign to the document.
        /// </param>
        /// <param name="content">
        ///     The UTF-8 text payload to store.
        /// </param>
        /// <returns>
        ///   The generated identifier of the newly stored content,
        ///   which can be used for subsequent retrieval or deletion.
        /// </returns>
        Task<string> InsertContentAsync(string name, Stream content);

        /// <summary>
        ///   Deletes the document identified by <paramref name="id"/> from storage.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the document to remove.
        /// </param>
        Task DeleteContentAsync(string id);

        /// <summary>
        ///   Retrieves all stored documents in one call.
        /// </summary>
        /// <returns>
        ///   A list of <see cref="StorageResponseBase"/> instances, each providing
        ///   the data stream and metadata for one stored document.
        /// </returns>
        Task<List<StorageResponseBase>> GetContentsAsync();
    }
}
