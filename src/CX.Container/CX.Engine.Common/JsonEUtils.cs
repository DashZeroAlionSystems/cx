using System.Text.Json.Nodes;
using Json.JsonE;

namespace CX.Engine.Common;

public static class JsonEUtils
{
    public static JsonNode AddCommonJsonEFunctions(this JsonNode context)
    {
        context["nz"] = Nz;
        context["filter"] = Filter;
        context["top"] = Top;
        return context;
    }

    public static JsonFunction Top = JsonFunction.Create((arguments, _) => {
        if (arguments.Length < 2)
            throw new InvalidOperationException("top requires at least two arguments");
        
        var arr = arguments[0] as JsonArray;

        if (arr == null)
            return arguments[0];

        var limit = arguments[1] as JsonValue;

        if (limit == null || !limit.TryGetValue(out decimal limitValue))
            throw new InvalidOperationException("top requires a valid limit");

        limitValue = (int)limitValue;
        
        if (limitValue < 0)
            throw new InvalidOperationException("top requires a non-negative limit");

        var res = new List<JsonNode>((int)limitValue);

        foreach (var item in arr)
        {
            if (res.Count == limitValue)
                break;

            res.Add(item.DeepClone());
        }

        return new JsonArray(res.ToArray());
    });

    public static JsonFunction Nz = JsonFunction.Create((arguments, ctx) =>
    {
        var defaultValue = arguments.Length > 1 ? arguments[1] : null;

        var path = arguments[0]?.ToString().Trim().NullIfWhiteSpace() ?? throw new InvalidOperationException("nz requires an identifier to evaluate");
        var parts = path.Split('.');
        if (parts.Length == 0)
            throw new InvalidOperationException("nz requires an identifier to evaluate");

        var rootId = parts[0];
        var rootPart = ctx.IsDefined(rootId) ? ctx.Find(rootId) : null;

        if (rootPart == null)
            return defaultValue;

        var curPart = rootPart;

        for (var j = 1; j < parts.Length; j++)
            if (rootPart is JsonObject jo)
            {
                if (jo.TryGetPropertyValue(parts[j], out var nestedPart))
                    curPart = nestedPart;
                else
                    return defaultValue;
            }
            else if (rootPart is JsonArray ja)
            {
                if (int.TryParse(parts[j], out var idx))
                {
                    if (idx < 0 || idx >= ja.Count)
                        return defaultValue;

                    curPart = ja[idx];
                }
                else
                    return defaultValue;
            }
            else
                return defaultValue;

        return curPart;
    });

    public static JsonFunction Filter = JsonFunction.Create((arguments, _) =>
    {
        if (arguments.Length < 2)
            throw new InvalidOperationException("filter requires at least two arguments");

        var arr = arguments[0] as JsonArray;

        if (arr == null)
            return arguments[0];

        var exp = arguments[1];

        if (exp == null)
            return arguments[0];
        
        var res = new List<JsonNode>(arr.Count);

        foreach (var item in arr)
        {
            var itemCtx = new JsonObject();
            itemCtx["item"] = item.DeepClone();
            itemCtx["ctx"] = arguments.ElementAtOrDefault(2);
            var expNode = new JsonObject();
            expNode["$eval"] = exp.DeepClone();
            var eval = JsonE.Evaluate(expNode, itemCtx.AddCommonJsonEFunctions());
            if (eval.GetTruthy())
                res.Add(item.DeepClone());
        }

        return new JsonArray(res.ToArray());
    });
}