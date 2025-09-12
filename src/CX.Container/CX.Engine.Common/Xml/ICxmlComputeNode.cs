namespace CX.Engine.Common.Xml;

public interface ICxmlComputeNode : ICxmlDependsOnProp
{
    /// <summary>
    /// NB: this method should not be called directly to prevent recursion and parallelism issues.
    /// Call <see cref="CxmlScope.ComputeAsync" /> or <see cref="ICxmlComputeNodeExt.ComputeAsync"/> or <see cref="Cxml.PerformComputeStagesAsync"/> to compute this node.
    /// </summary>
    public Task InternalComputeAsync(CxmlScope scope);
}

public static class ICxmlComputeNodeExt
{
    public static Task ComputeAsync(this ICxmlComputeNode node, CxmlScope scope)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
        
        return scope.ComputeAsync(node);
    }
}