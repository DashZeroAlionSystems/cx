namespace CX.Engine.Common.Xml;

public class CxmlDependencyTracker<TOperation, TNode>
    where TOperation: CxmlDependencyTracker<TOperation, TNode>, new()
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly AsyncLocal<CxmlDependencyTracker<TOperation, TNode>> _activeOp = new();

    public static CxmlDependencyTracker<TOperation, TNode> ActiveOp => _activeOp.Value; 

    public Dictionary<TNode, Task> PerNodeTasks;
    
    private HashSet<TNode> NodesInOperation { get; } = [];

    public static CxmlDependencyTracker<TOperation, TNode> Start(string reentryErrorMessage, Dictionary<TNode, Task> perNodeTasks)
    {
        if (_activeOp.Value != null)
            throw new CxmlException(reentryErrorMessage); 

        return _activeOp.Value = new() { PerNodeTasks = perNodeTasks };
    }
    
    public void EnterNode(TNode node)
    {
        if (!NodesInOperation.Add(node))
            throw new CxmlCircularDependencyException($"Circular dependency detected at {node}.");
    }
    
    public Task ProcessOnceAsync(TNode node, Func<TNode, Task> func)
    {
        Task t;
        lock (PerNodeTasks)
        {
            if (PerNodeTasks.TryGetValue(node, out var task))
                t = task;
            else
                t = PerNodeTasks[node] = func(node);
        }

        return t;
    }
    
    public async Task WalkInStartedAsync(TNode node,
        Func<TNode, IEnumerable<TNode>> getDependencies,
        Func<TNode, Task> processAsync)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        EnterNode(node);

        // Process dependencies concurrently.
        await CxTask.WhenAll(getDependencies(node)?.Select(dep => WalkInStartedAsync(dep, getDependencies, processAsync)));

        await ProcessOnceAsync(node, processAsync);
    }
    
    public async Task WalkAsync(TNode node,
        Dictionary<TNode, Task> perNodeTasks,
    string reentryErrorMessage, 
        Func<TNode, IEnumerable<TNode>> getDependencies,
        Func<TNode, Task> processAsync)
    {
        Start(reentryErrorMessage, perNodeTasks);
        await WalkInStartedAsync(node, getDependencies, processAsync);
    }
}