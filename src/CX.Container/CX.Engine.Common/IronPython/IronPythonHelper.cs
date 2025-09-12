using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using IronPython.Runtime;

namespace CX.Engine.Common.IronPython;

public static class IronPythonHelper
{
    public static async Task ResolveArrayAsync(List<string> lst, IronPythonContext ctx = null)
    {
        ctx ??= IronPythonContext.GetDefaultContext();

        for (var i = 0; i < lst.Count; i++)
        {
            var element = lst[i];
            if (element.StartsWith("ironpython:"))
            {
                var script = element.Substring("ironpython:".Length);
                var result = await IronPythonExecutor.ExecuteScriptAsync(ctx.GetRequest(script));

                result = await MiscHelpers.AwaitAnyAsync(result, true);

                if (result is string s)
                    lst[i] = s;
                else if (result is List<string> el_lst)
                {
                    lst.RemoveAt(i);

                    if (el_lst.Count > 0)
                        lst.InsertRange(i, el_lst);
                }
                else if (result is PythonList el_pyLst)
                {
                    lst.RemoveAt(i);

                    if (el_pyLst.Count > 0)
                        lst.InsertRange(i, el_pyLst.Cast<string>());
                }
                else
                    throw new InvalidOperationException($"Invalid result type for string array unfolding: {result?.GetType().FullName ?? "<null>"}");
            }
        }
    }

    public static JsonNode ToJsonNode(dynamic val)
    {
        if (val == null)
            return (JsonValue)null;
        else if (val is string s)
            return JsonValue.Create(s);
        else if (val is double d)
            return JsonValue.Create(d);
        else if (val is int i)
            return JsonValue.Create(i);
        else if (val is bool b)
            return JsonValue.Create(b);
        else if (val is List<string> el_lst)
        {
            var res = new JsonArray();
            foreach (var el in el_lst)
                res.Add(JsonValue.Create(el));
            return res;
        }
        else if (val is PythonList el_pyLst)
        {
            var res = new JsonArray();
            if (el_pyLst.Count > 0)
                foreach (var el in el_pyLst.Cast<string>())
                    res.Add(JsonValue.Create(el));
            return res;
        }
        else
            throw new NotSupportedException($"Unsupported value type: {val.GetType().FullName ?? "<null>"}");
    }


    public static async Task<JsonNode> ResolveValueAsync(JsonValue el, IronPythonContext ctx = null)
    {
        ctx ??= IronPythonContext.GetDefaultContext();

        if (el.GetValueKind() == JsonValueKind.String)
        {
            var jvs = el.GetValue<string>();
            if (jvs.StartsWith("ironpython:"))
            {
                var script = jvs.Substring("ironpython:".Length);
                var result = await IronPythonExecutor.ExecuteScriptAsync(ctx.GetRequest(script));

                result = await MiscHelpers.AwaitAnyAsync(result, true);
                return ToJsonNode(result);
            }
            else
                return el;
        }
        else
        {
            return el;
        }
    }

    public static async Task ResolveJsonArrayAsync(JsonArray arr, IronPythonContext ctx = null)
    {
        ctx ??= IronPythonContext.GetDefaultContext();

        for (var i = 0; i < arr.Count; i++)
        {
            var element = arr[i];
            if (element is JsonValue jv)
            {
                if (jv.GetValueKind() != JsonValueKind.String)
                    continue;

                var jvs = jv.GetValue<string>();
                if (!jvs.StartsWith("ironpython:"))
                    continue;

                var script = jvs.Substring("ironpython:".Length);
                var result = await IronPythonExecutor.ExecuteScriptAsync(ctx.GetRequest(script));

                result = await MiscHelpers.AwaitAnyAsync(result, true);

                if (result is List<string> el_lst)
                {
                    arr.RemoveAt(i);

                    if (el_lst.Count > 0)
                        arr.InsertRange(i, el_lst.Select(el => JsonValue.Create(el)));
                }
                else if (result is PythonList el_pyLst)
                {
                    arr.RemoveAt(i);

                    if (el_pyLst.Count > 0)
                        arr.InsertRange(i, el_pyLst.Select(el => ToJsonNode(el)));
                }
                else
                    arr[i] = ToJsonNode(result);
            }
        }
    }

    public static async Task ResolveJsonObjectAsync(JsonObject obj, IronPythonContext ctx = null)
    {
        ctx ??= IronPythonContext.GetDefaultContext();

        foreach (var kvp in obj.ToList())
        {
            obj[kvp.Key] = await ResolveJsonNodeAsync(kvp.Value, ctx);
        }
    }

    public static async Task<JsonNode> ResolveJsonNodeAsync(JsonNode input, IronPythonContext ctx = null)
    {
        ctx ??= IronPythonContext.GetDefaultContext();

        if (input is JsonArray arr)
        {
            await ResolveJsonArrayAsync(arr, ctx);
            return arr;
        }
        else if (input is JsonObject jo)
        {
            await ResolveJsonObjectAsync(jo, ctx);
            return jo;
        }
        else if (input is JsonValue jv)
        {
            return await ResolveValueAsync(jv, ctx);
        }

        return input;
    }
    public static async Task<object> ResolveObjectAsync(object context)
    {
        // 1. If it's an ExpandoObject or IDictionary<string, object>, handle each entry
        if (context is IDictionary<string, object> dict)
        {
            var keys = dict.Keys.ToArray(); // copy to avoid modifying while iterating
            foreach (var key in keys)
            {
                var val = dict[key];
                dict[key] = await MiscHelpers.AwaitAnyAsync(val);
            }
        }
        // 2. Otherwise, reflect over the object's properties
        else
        {
            var props = context.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);
            
            if(!props.Any())
                return await MiscHelpers.AwaitAnyAsync(context, true);
            
            foreach (var prop in props)
            {
                var val = prop.GetValue(context);
                var awaitedValue = await MiscHelpers.AwaitAnyAsync(val);
                prop.SetValue(context, awaitedValue);
            }
        }

        return null;
    }
}