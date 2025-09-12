using CX.Engine.Common.Stores.Graphs;
using JetBrains.Annotations;

namespace Cx.Engine.Common.PromptBuilders;

public class EdgePromptBuilder<TFocal> : PromptBuilder where TFocal: class
{
    public readonly PromptContentSection Instructions = new("", 0, false);
    public readonly PromptContentSection FocalObjectDescription = new("", 1, false);
    public readonly Dictionary<Type, Func<object, PromptContentSection>> OutboundEdgeFns = new();
    public readonly Dictionary<Type, Func<object, PromptContentSection>> InboundEdgeFns = new();
    public Func<TFocal, PromptContentSection> SelfEdgeFn;
    public readonly TFocal FocalObject;
    
    public EdgePromptBuilder([NotNull] TFocal focalObject)
    {
        FocalObject = focalObject ?? throw new ArgumentNullException(nameof(focalObject));
        Context.FocalObject = focalObject;
        Add(Instructions);
        Add(FocalObjectDescription);
    }
    
    public EdgePromptBuilder<TFocal> ForOutboundEdgesOfType<T>(Func<T, PromptContentSection> function)
    {
        OutboundEdgeFns[typeof(T)] = o => function((T)o);
        return this;
    }

    public EdgePromptBuilder<TFocal> ForInboundEdgesOfType<T>(Func<T, PromptContentSection> function)
    {
        InboundEdgeFns[typeof(T)] = o => function((T)o);
        return this;
    }

    public EdgePromptBuilder<TFocal> ForSelfEdges(Func<TFocal, PromptContentSection> function)
    {
        SelfEdgeFn = o => function(o);
        return this;
    }

    public void AddEdge(ObjectEdge edge)
    {
        if (edge.Source != FocalObject && edge.Target != FocalObject)
            throw new NotSupportedException("Edge does not involve the focal object");
        
        if (edge.Source == null || edge.Target == null)
            throw new NotSupportedException("Edge has null source or target");
        
        if (edge.Source == FocalObject && edge.Target == FocalObject)
            SelfEdgeFn?.Invoke(FocalObject);
        
        //outbound edges
        if (edge.Source == FocalObject)
        {
            if (OutboundEdgeFns.TryGetValue(edge.Target.GetType(), out var fn))
                Add(fn(edge.Target));
        }

        //inbound edges
        if (edge.Target == FocalObject)
        {
            if (InboundEdgeFns.TryGetValue(edge.Source.GetType(), out var fn))
                Add(fn(edge.Source));
        }
    }

    public EdgePromptBuilder<TFocal> AddEdges(ObjectEdges edges) => AddEdges(edges.Edges);

    public EdgePromptBuilder<TFocal> AddEdges(IEnumerable<ObjectEdge> edges)
    {
        foreach (var edge in edges)
        {
            AddEdge(edge);
        }

        return this;
    }
}