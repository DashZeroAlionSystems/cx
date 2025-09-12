namespace CX.Engine.Common.Xml;

public interface ICxmlHasParentProp
{
    object Parent { get; set; }
}

public static class ICxmlHasParentPropExt
{
    public static T GetAncestor<T>(this ICxmlHasParentProp node, bool includeSelf = false)
    {
        if (includeSelf && node is T tSelf)
            return tSelf;
        
        if (node.Parent == null)
            return default;

        if (node.Parent is T tParent)
            return tParent;

        if (node.Parent is ICxmlHasParentProp parent)
            return GetAncestor<T>(parent);

        return default;
    }

    public static List<T> GetAncestors<T>(this ICxmlHasParentProp node, bool includeSelf = false)
    {
        var res = new List<T>();
        if (includeSelf && node is T t)
            res.Add(t);
        GetAncestors(node, res);
        return res;
    }

    public static void GetAncestors<T>(this ICxmlHasParentProp node, List<T> lst)
    {
        if (node.Parent == null)
            return;

        if (lst == null)
            throw new ArgumentNullException(nameof(lst));

        if (node.Parent is T t)
            lst.Add(t);

        if (node.Parent is ICxmlHasParentProp parent)
            GetAncestors(parent, lst);
    }

    public static List<PromptSectionNode> ResolvePromptSections(this ICxmlHasParentProp node)
    {
        List<PromptSectionNode> res = [];

        var scopes = node.GetAncestors<ICxmlPromptScope>();
        foreach (var scope in scopes)
            if (scope.PromptSections != null)
                foreach (var section in scope.PromptSections)
                    res.Add(section);

        if (node is ICxmlHasPromptSectionsProp ps && ps.PromptSections != null)
            foreach (var section in ps.PromptSections)
                res.Add(section);

        return res;
    }
}