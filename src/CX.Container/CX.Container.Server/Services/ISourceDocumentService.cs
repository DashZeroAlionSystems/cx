using CX.Container.Server.Domain.Nodes.Dtos;

namespace CX.Container.Server.Services;

public interface ISourceDocumentService
{
    Task UpdateOrCreateSourceDocumentAsync(Guid nodeId, NodeForUpdateS3Dto nodeForUpdateS3);
}
