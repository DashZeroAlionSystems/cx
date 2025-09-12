using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using CX.Engine.Common.IronPython;
using Json.More;

namespace CX.Engine.Common.Json;

public class JsonEval
{
    public readonly object Lock = new();

    public JsonEval()
    {
    }

    public async Task EvalAsync(JsonNode node, string cmd)
    {
        var res = await IronPythonExecutor.ExecuteScriptAsync(cmd, new { node = node });

        if (res is string)
            node.ReplaceWith(JsonValue.Create(res));
    }

    public async Task EvalAsync(JsonNode node)
    {
        //check if this node is a string starting with ::
        if (node is JsonValue jv && jv.GetValueKind() == JsonValueKind.String)
        {
            var s = jv.GetString()!;

            if (s.StartsWith("ironpython:"))
            {
                s = s.Substring("ironpython:".Length);
                await EvalAsync(node, s);
            }

            if (s.StartsWith("\\ironpython:"))
            {
                s = s[1..];
                lock (Lock)
                    jv.ReplaceWith(JsonValue.Create(s));
            }
        }

        if (node is JsonObject jo)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < jo.Count; i++)
            {
                var prop = jo[(Index)i];
                tasks.Add(EvalAsync(prop));
            }

            await tasks;
        }

        if (node is JsonArray ja)
        {
            var tasks = new List<Task>();

            for (var i = 0; i < ja.Count; i++)
            {
                var item = ja[(Index)i];
                tasks.Add(EvalAsync(item));
            }

            await tasks;
        }
    }
}