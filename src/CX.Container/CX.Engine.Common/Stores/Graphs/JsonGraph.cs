using CX.Engine.Common.Stores.Json;
using JetBrains.Annotations;

namespace CX.Engine.Common.Stores.Graphs;

public class JsonGraph
{
    public JsonObjectStore Objects;
    public JsonEdgeStore Edges;
    
    public JsonGraph([NotNull] JsonObjectStore objects, [NotNull] JsonEdgeStore edges)
    {
        Objects = objects ?? throw new ArgumentNullException(nameof(objects));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
    }

    public async Task<List<ObjectEdge>> GetEdgesForKeyAsync(string key) => await Objects.ResolveEdgesAsync(await Edges.GetEdgesForKeyAsync(key));
    public async Task<ObjectEdges> GetEdgesForResolvedKeyAsync(string key)
    {
        var obj = (IStoreObject)await Objects.GetAsync(key);
        return await GetEdgesForObjectAsync(obj);
    }

    public async Task<ObjectEdges> GetEdgesForObjectAsync(IStoreObject obj) =>
        new (obj, await Objects.ResolveEdgesAsync(await Edges.GetEdgesForKeyAsync(obj.StoreKey), new()
        {
            [obj.StoreKey] = Task.FromResult<object>(obj)
        }));
}