namespace CX.Engine.Common.Xml;

public interface ICxmlChildren
{
    public IEnumerable<object> CxmlChildren();
}

public static class ICxmlChildrenExt
{
    public static IEnumerable<T> ChildrenOfType<T>(this ICxmlChildren root) => root.CxmlChildren().OfType<T>();
    public static IEnumerable<T> DescendantsOfType<T>(this ICxmlChildren root) => root.CxmlChildren().SelectMany(o => o is T t ? [t] : o is ICxmlChildren c ? c.DescendantsOfType<T>() : Array.Empty<T>());
    
    public static IEnumerable<object> ChildrenWithId(this ICxmlChildren root, string id) => root.CxmlChildren().Where(o => o is ICxmlId i && i.Id == id);
    public static IEnumerable<object> DescendantsWithId(this ICxmlChildren root, string id) => root.CxmlChildren().SelectMany(o => o is ICxmlId i && i.Id == id ? [o] : o is ICxmlChildren c ? c.DescendantsWithId(id) : Array.Empty<object>());
    
    public static IEnumerable<KeyValuePair<string, object>> DescendantsWithIds(this ICxmlChildren root) => root.CxmlChildren().SelectMany(o => o is ICxmlId i && !string.IsNullOrWhiteSpace(i.Id) ? [ new(i.Id, o) ] : o is ICxmlChildren c ? c.DescendantsWithIds() : Array.Empty<KeyValuePair<string, object>>());
    
}