using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CX.Engine.Common.CodeProcessing;
using CX.Engine.Common.Conversion;
using CX.Engine.Common.Formatting;
using CX.Engine.Common.Reflection;
using CX.Engine.Common.Rendering;
using HtmlAgilityPack;
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConstantConditionalAccessQualifier

namespace CX.Engine.Common.Xml;

public static class Cxml
{
    public static readonly object NotFound = new();

    public static IEnumerable<HtmlNode> DescendantsWithTag(this HtmlNode root, List<string> tags) => root.Descendants().Where(n => tags.Contains(n.Name));
    public static IEnumerable<HtmlNode> ChildrenWithTag(this HtmlNode root, List<string> tags) => root.ChildNodes.Where(n => tags.Contains(n.Name));
    public static IEnumerable<HtmlNode> ChildrenWithTag(this HtmlNode root, string tag) => root.ChildNodes.Where(n => tag == n.Name);
    public static HtmlNode FirstDescendantsWithTag(this HtmlNode root, List<string> tags) => root.Descendants().FirstOrDefault(n => tags.Contains(n.Name));

    public static async IAsyncEnumerable<object> ProcessChildrenAsync(this HtmlNode root, CxmlScope scope, List<string> tags, CxmlChildrenAttribute attr,
        List<string> ignoreTags, HashSet<HtmlNode> usedChildren)
    {
        if (attr.All)
        {
            foreach (var child in root.ChildNodes)
            {
                if (ignoreTags != null && ignoreTags.Contains(child.Name))
                    continue;

                if (tags.Contains(child.Name))
                {
                    if (usedChildren.Add(child))
                        yield return await ParseToObjectAsync(child, scope);
                }
                else if (attr.UnknownAsStrings)
                {
                    if (usedChildren.Add(child))
                    {
                        var html = child.BasicRenderToString();

                        if (html != null)
                        {
                            yield return html;
                            continue;
                        }
                    }
                }
                else
                if (usedChildren.Add(child))
                    yield return child;
            }

            yield break;
        }

        foreach (var child in root.ChildrenWithTag(tags))
        {
            var obj = await ParseToObjectAsync(child, scope);

            if (obj == null && attr.UnknownAsStrings)
            {
                yield return child.BasicRenderToString(innerText: true);
            }
            else
            {
                if (obj == null)
                    continue;

                yield return obj;
            }
        }
    }

    public static ConcurrentDictionaryCache<List<string>, Regex> CxmlTagUsesOpenAndClosedCache = new(tags =>
    {
        var escapedTags = string.Join("|", tags.Select(Regex.Escape));
        var pattern = $@"<(?:{escapedTags})[^>]*>.*?</(?:{escapedTags})>";
        return new(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }, new(new StringJoinComparer<List<string>>()));

    public static ConcurrentDictionaryCache<List<string>, Regex> CxmlTagUsesSingleElementCache = new(tags =>
    {
        var escapedTags = string.Join("|", tags.Select(Regex.Escape));
        //patern should include the /> at the end
        var pattern = $@"<(?:{escapedTags})[^>]*\/>";
        return new(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }, new(new StringJoinComparer<List<string>>()));

    public static string BasicRenderToString(this HtmlNode node, bool innerText = false)
    {
        var sb = new StringBuilder();
        node.BasicRender(sb, innerText);
        return sb.ToString();
    }

    /// <summary>
    /// Only renders basic HTML nodes (br and text).
    /// </summary>
    public static void BasicRender(this HtmlNode node, StringBuilder sb, bool innerText = false)
    {
        if (node.Name == "br")
        {
            sb.AppendLine();
        }
        else if (node is HtmlTextNode text)
        {
            sb.Append(HtmlEntity.DeEntitize(text.InnerText.RemoveCommonIndentation()?.Trim()!));
        }
        else
            sb.Append(innerText ? node.InnerText : node.OuterHtml);
    }

    public static async Task<string> RenderToStringAsync(this object o, CxmlScope scope = null, bool smartFormat = false, bool outputException = true)
    {
        var sb = new StringBuilder();
        var ctx = TextRenderContext.InheritOrNew(sb);
        ctx.Scope = scope ?? new();
        await o.RenderToTextContextAsync();
        var res = sb.ToString();
        if (smartFormat)
            try
            {
                res = await CxSmart.LazyFormatAsync(res, scope);
            }
            catch (Exception ex) when (outputException)
            {
                res = ex.Message;
            }
        return res;
    }

    public static Task<string> EvalStringAsync(string input, CxmlScope scope)
    {
        if (input == null)
            return null;
        
        var doc = new HtmlDocument();
        doc.LoadHtml(input);
        return EvalStringAsync(doc.DocumentNode, scope);
    }

    public static async Task<string> EvalStringAsync(HtmlNode node, CxmlScope scope)
    {
        scope.SmartFormatEscape = true;
        var obj = await ParseToObjectAsync(node, scope);

        string s;
        
        if (obj == null)
            s = node.BasicRenderToString();
        else
        {
            var ctx = TextRenderContext.Current = new();
            ctx.Scope = scope;
            
            var sb = new StringBuilder();
            ctx.StringBuilders.Push(sb);

            await PerformComputeStagesAsync(obj, scope);
            await obj.RenderToTextContextAsync();

            s = sb.ToString();
        }
        
        s = await CxSmart.LazyFormatAsync(s, scope);

        return s;
    }

    public static async Task<object> ProcessIdentifierAsync(this HtmlNode root, CxmlScope scope, string attrName, Type valueType, CxmlContentAttribute contentAttr,
        CxmlChildrenByNameAttribute childrenByNameAttr, Func<object> onNotFound, HashSet<HtmlNode> usedChildren)
    {
        var processed = false;

        if (attrName == "scope" && typeof(CxmlScope).IsAssignableTo(valueType))
            return scope;
        if (attrName == "node" && valueType == typeof(HtmlNode))
            return root;
        if (attrName == "context" && valueType == typeof(StubbedLazyDictionary))
            return scope.Context;
        if (attrName == "document" && valueType == typeof(HtmlNode))
            return root.OwnerDocument.DocumentNode;

        object value = root.GetAttributeValue(attrName.ToKebabCase(), null!);

        // NB: bad Resharper heuristics on importer library here
        // ReSharper disable HeuristicUnreachableCode
        // ReSharper disable ConditionIsAlwaysTrueOrFalse

        // If this is the content argument (first parameter) and no attribute is found, use the inner text.
        if (contentAttr != null)
        {
            if (valueType == typeof(List<HtmlNode>))
                value = root.ChildNodes.ToList();
            else
            {
                if (contentAttr.Trim)
                    value ??= HtmlEntity.DeEntitize(root.InnerText?.Trim());
                else
                    value ??= HtmlEntity.DeEntitize(root.InnerText);
            }
        }
        else if (childrenByNameAttr != null)
        {
            if (valueType!.IsAssignableTo(typeof(IList)))
            {
                var valueElementType = typeof(object);

                if (valueType.IsGenericType && valueType.GenericTypeArguments.Length > 0)
                    valueElementType = valueType.GetGenericArguments()[0];
                
                if (value == null)
                {
                    var lst = await ProcessChildrenAsync(root, scope, [childrenByNameAttr.Name], new() { All = false, UnknownAsStrings = true }, null, usedChildren).ToListAsync();
                    var resLst = (IList)Activator.CreateInstance(valueType);
                    foreach (var o in lst)
                    {
                        if (childrenByNameAttr.References)
                        {
                            var val = scope.ResolveReferenceFieldValueAsync(valueElementType, o);
                            if (val != null)
                                resLst.Add(o);
                        }
                        else
                            resLst.Add(await CxConvert.ToAsync(valueElementType, o));
                    }

                    value = resLst;
                }
            }
            else
                throw new InvalidOperationException("Children attribute can only be used with List<object>.");
        }
        else
        {
            if (value == null)
            {
                var child = root.ChildNodes.FindFirst(attrName.ToKebabCase());
                if (child != null)
                    if (valueType == typeof(HtmlNode))
                        value = child;
                    else
                        value = child.AsStringContent();
            }
        }

        if (value == null)
            return onNotFound();

        // ReSharper restore ConditionIsAlwaysTrueOrFalse
        // ReSharper restore HeuristicUnreachableCode

        {
            if (value is string vs && vs.StartsWith("{") && vs.EndsWith("}"))
            {
                processed = true;
                var inside = vs.Substring(1, vs.Length - 2);
                value = await Accessor.EvaluateAsync(inside, scope);
            }
        }

        {
            if (!processed && value is string vs && vs.StartsWith("raw:"))
            {
                processed = true;
                value = vs.Substring(4).SmartFormatEscape(scope.SmartFormatEscape);
            }
        }

        if (valueType != typeof(object))
            value = await CxConvert.ToAsync(valueType, value);

        return value;
    }

    public static async Task<T> ParseToObjectAsync<T>(string content, CxmlScope scope)
    {
        var obj = await ParseToObjectAsync(content, scope);

        if (obj is T objT)
            return objT;

        if (obj is CxmlContainerNode cn)
        {
            var x = cn.ChildrenOfType<T>().FirstOrDefault();
            if (x != null)
                return x;
        }

        // ReSharper disable once PossibleInvalidCastException
        return (T)obj;
    }

    public static Task<object> ParseToObjectAsync(string content, CxmlScope scope)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<object>(null);
        
        var doc = new HtmlDocument();
        doc.LoadHtml(content?.Trim());

        return ParseToObjectAsync(doc.DocumentNode, scope);
    }

    public static async Task<object> ParseToObjectAsync(HtmlNode node, CxmlScope scope)
    {
        var tags = scope.Preparation.MethodNames;

        // Get the name of the root tag
        var tag = node.Name;

        Delegate method = null;

        scope.Context.SetToInitMode();
        var parentScope = scope;
        scope = scope.Inherit();
        scope.Context["node"] = node;
        scope.Context["scope"] = scope;
        scope.Context["context"] = scope.Context;
        scope.Context["document"] = node.OwnerDocument.DocumentNode;

        if (tags.Contains(tag))
            method = scope.Preparation.Delegates[tag];
        else if (parentScope.TopLevelNodeHandler != null)
            method = parentScope.TopLevelNodeHandler;

        if (method == null)
            return null;

        // Create an object[] for the arguments of the method by checking the method delegate's parameters
        var pars = method.Method.GetParameters();
        var args = new object[pars.Length];
        var parNames = pars.Select(p => p.Name.ToKebabCase()).ToList();

        HashSet<HtmlNode> usedChildren = [];

        // Read arguments from the HTML
        for (var i = 0; i < pars.Length; i++)
        {
            var par = pars[i];
            
            if (par.HasAttribute<CxmlIgnoreAttribute>())
                continue;
            
            var isRequired = par.HasAttribute<CxmlRequiredAttribute>();
            args[i] = await node.ProcessIdentifierAsync(scope, par.Name!, par.ParameterType, par.GetAttribute<CxmlContentAttribute>(),
                par.GetAttribute<CxmlChildrenByNameAttribute>(),
                () => NotFound, usedChildren);

            if (par.HasAttribute<CxmlChildrenAttribute>(out var attr))
            {
                var valueFromChildren = await node.ProcessChildrenAsync(scope, tags, attr, parNames, usedChildren).ToListAsync();

                if (!valueFromChildren.IsEmptyOrNull())
                    if (args[i] == NotFound)
                        args[i] = valueFromChildren;
                    else if (args[i] is IEnumerable<object> lst)
                    {
                        List<object> newValue = [..valueFromChildren, ..lst];
                        args[i] = newValue;
                    }
            }

            if (args[i] == NotFound)
            {
                if (isRequired)
                    throw new ArgumentException($"Method {method.Method.Name} requires an argument {par.Name}.");

                if (par.HasDefaultValue)
                    args[i] = par.DefaultValue;
                else
                    args[i] = par.ParameterType.IsValueType ? Activator.CreateInstance(par.ParameterType) : null;
            }
        }

        var res = method.DynamicInvoke(args);

        res = await MiscHelpers.AwaitAnyAsync(res);

        if (method.Method.GetAttribute<CxmlFactoryAttribute>() != null && res != null)
        {
            var resType = res.GetType();

            if (res is IDictionary<string, object> dict)
            {
                foreach (var attr in node.Attributes)
                    dict[attr.Name] = await node.ProcessIdentifierAsync(scope, attr.Name, typeof(object), null, null, () => null, usedChildren);
            }
            else
            {
                var allProps = resType.GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public);
                var allPropNames = allProps.Select(p => p.Name.ToKebabCase()).ToList();
                foreach (var prop in allProps)
                {
                    if (prop.HasAttribute<CxmlIgnoreAttribute>())
                        continue;

                    var propName = prop.Name;
                    if (prop.TryGetAttribute<CxmlFieldAttribute>(out var fieldAttr))
                    {
                        if (fieldAttr.Name != null)
                            propName = fieldAttr.Name;
                    }
                    
                    List<object> valueFromChildren = null;

                    if (prop.HasAttribute<CxmlChildrenAttribute>(out var attr))
                        valueFromChildren = await node.ProcessChildrenAsync(scope, tags, attr, allPropNames, usedChildren).ToListAsync();

                    var valueFromProps =
                        await node.ProcessIdentifierAsync(scope, propName, prop.ValueType, prop.GetAttribute<CxmlContentAttribute>(),
                            prop.GetAttribute<CxmlChildrenByNameAttribute>(),
                            () => NotFound, usedChildren);

                    if (prop.HasAttribute<CxmlRequiredAttribute>() && valueFromProps == NotFound && valueFromChildren.IsEmptyOrNull())
                        throw new CxmlException($"{resType.FullName} requires property {prop.Name} to be set in CXML.");

                    var value = valueFromProps;
                    if (!valueFromChildren.IsEmptyOrNull())
                    {
                        if (value is IEnumerable<object> list)
                        {
                            List<object> newValue = [..valueFromChildren!, ..list];
                            value = newValue;
                        }

                        if (value == NotFound)
                            value = valueFromChildren;
                    }

                    if (value != NotFound)
                        prop.SetValue(res, await CxConvert.ToAsync(prop.ValueType, value));
                }
            }
        }

        if (res is ICxmlAddChild resRoot)
        {
            var attr = method.Method.ReturnType.GetCustomAttribute<CxmlChildrenAttribute>();
            attr ??= new(true);
            var allProps = res.GetType().GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public);
            var allPropNames = allProps.Select(p => p.Name.ToKebabCase()).ToList();
            var valueFromChildren = await node.ProcessChildrenAsync(scope, tags, attr, allPropNames, usedChildren).ToListAsync();

            foreach (var child in valueFromChildren)
                await resRoot.AddChildAsync(child);
        }

        return res;
    }

    public static async Task FormatCxmlNodeAsync(HtmlNode node, CxmlScope scope)
    {
        var sb = new StringBuilder();
        var ctx = TextRenderContext.InheritOrNew(sb);

        scope = scope.Inherit();
        scope.SmartFormatEscape = true;
        await RenderCxmlNodeAsync(node, scope);

        var s = sb.ToString();
        s = await CxSmart.LazyFormatAsync(s, scope.Context);
        ctx.Parent.Sb.Append(s);
    }

    public static async Task RenderCxmlNodeAsync(HtmlNode node, CxmlScope scope)
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;
        if (node is HtmlTextNode textNode)
        {
            sb.AppendLine(HtmlEntity.DeEntitize(textNode.InnerText.RemoveCommonIndentation()?.Trim()!));
            return;
        }

        var obj = await ParseToObjectAsync(node, scope);
        await obj.RenderToTextContextAsync();
    }

    public static async Task<string> EvalStringAsync(string s, object context, params Delegate[] delegates)
    {
        var scope = new CxmlScope(new(context), delegates);
        return await EvalStringAsync(s, scope);
    }

    public static async Task RenderToTextContextAsync(this IEnumerable children)
    {
        if (children == null)
            throw new ArgumentNullException(nameof(children));
        
        foreach (var c in children)
            await c.RenderToTextContextAsync();
    }

    public static async Task RenderToTextContextAsync(this object o)
    {
        if (o == null)
            return;

        if (o.IsVoid())
            return;

        if (o is IRenderToText render)
        {
            await render.RenderToTextAsync();
            return;
        }

        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;

        if (o is string)
        {
            sb.Append(o);
            return;
        }

        if (o is IEnumerable children)
        {
            await children.RenderToTextContextAsync();
            return;
        }
        
        if (o is HtmlNode node)
        {
            BasicRender(node, sb);
            return;
        }

        sb.Append(o);
    }

    public static async Task PerformComputeStagesAsync(object o, CxmlScope scope)
    {
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));
        
        scope = scope.Inherit();
        if (o is ICxmlChildren parent)
            scope.Context["nodes"] = new NodeSelector(parent);
        
        //set parents
        await VisitAllAsync<ICxmlChildren>(o, async parent =>
        {
            foreach (var child in parent.CxmlChildren())
                if (child is ICxmlHasParentProp cxmlParent)
                    cxmlParent.Parent = parent;
        });
        
        //resolve dependencies
        await VisitAllAsync<ICxmlDependsOnProp>(o, async l =>
        {
            var deps = l.DependsOn;
            for (var i = 0; i < deps.Count; i++)
            {
                var dep = deps[i];
                if (dep is ICxmlDependsOnProp)
                    continue;

                var accessor = await dep.RenderToStringAsync();
                if (string.IsNullOrWhiteSpace(accessor))
                    deps[i] = null;
                else
                    deps[i] = await Accessor.EvaluateAsync(accessor, scope);
            }
        });
        
        await VisitAllAsync<ICxmlComputeNode>(o, l => l.ComputeAsync(scope));
    }

    /// <summary>
    /// - Resolves br elements to newlines.
    /// - De-entitizes. 
    /// - Trims other elements' content.
    /// - Removes common indentation on a per-node basis.
    /// </summary>
    public static string AsStringContent(this HtmlNode node)
    {
        StringBuilder sb = new();

        foreach (var child in node.ChildNodes)
            child.BasicRender(sb);

        return sb.ToString();
    }

    /// <summary>
    /// Visit this node and all of its <see ref="ICxmlChildren" /> descendants.
    /// </summary>
    public static async Task VisitAllAsync<T>(object o, Func<T, Task> visitAsync)
    {
        if (visitAsync == null)
            throw new ArgumentNullException(nameof(visitAsync));
        
        if (o is T t)
            await visitAsync(t);

        if (o is ICxmlChildren parent)
            await Task.WhenAll(from child in parent.CxmlChildren() select VisitAllAsync(child, visitAsync));
    }
}