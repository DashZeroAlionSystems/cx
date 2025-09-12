namespace CX.Engine.Common.Xml;

public interface ICxmlDependsOnProp
{
    [CxmlChildrenByName("depends-on")]
    public List<object> DependsOn { get; set; }
}

public static class ICxmlDependOnPropExt
{
    public static HashSet<T> ResolveDependencies<T>(this ICxmlDependsOnProp node)
    {
        HashSet<T> res = [];

        if (node is ICxmlHasParentProp pnode)
        {
            var scopes = pnode.GetAncestors<ICxmlDependencyScope>();
            foreach (var scope in scopes)
            foreach (var dep in scope.DependsOn)
                if (dep is T t && dep != node)
                    res.Add(t);
        }
        
        foreach (var dep in node.DependsOn)
            if (dep is T t)
                res.Add(t);

        return res;
    }
}