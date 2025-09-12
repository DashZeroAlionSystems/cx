using CX.Engine.Assistants.AssessmentBuilder;
using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using Cx.Engine.Common.PromptBuilders;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Xml;
using HtmlAgilityPack;

namespace CX.Engine.Assistants.AssessmentBuilder;

public class SimpleQuestion : BaseNode, IRenderToText, ICxmlId, ICxmlComputeNode, ICxmlPromptScope
{
    public List<object> DependsOn { get; set; }

    [CxmlChildrenByName("prompt-section")]
    public List<PromptSectionNode> PromptSections { get; set; } = [];

    public const string TypeId = "simple-question";
    
    public string Question;
    public int Marks;
    public HtmlNode Prompt;
    public string Id { get; set; }
    public string FullId => $"{TypeId}.{Id}";
    public string SchemaName => TypeId.ToValidOpenAISchemaName();
    
    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;
        sb.Append(Question);
        if (Marks > 0)
            sb.Append($" ({Marks})");
    }

    public Task InternalComputeAsync(CxmlScope scope) =>
        CXTrace.Current.SpanFor(FullId, new { Prompt = Prompt?.InnerHtml?.Trim() }).ExecuteAsync(async _ =>
        {
            if (Prompt == null)
                return;

            scope = scope.Inherit();
            scope["question"] = Question;
            scope["marks"] = Marks;
            scope.TopLevelNodeHandler = CxmlCommon.ContainerNode;
            
            var prompt = await Cxml.EvalStringAsync(Prompt, scope);
            
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            var pb = new PromptBuilder();
            var sections = this.ResolvePromptSections();            
            await pb.AddAsync(sections, scope);

            pb.Add(prompt, 10_000);
            prompt = pb.GetPrompt();
            
            var utils = await GetUtilsAsync(scope);
            var chatAgent = utils.Snapshot.ChatAgent;

            var req = chatAgent.GetRequest(prompt);
            req.StringContext.Add(await utils.ContextForPromptAsync(prompt)); 
            var schema = chatAgent.GetSchema(SchemaName);
            schema.Object.AddProperty("reasoning", PrimitiveTypes.String);
            schema.Object.AddProperty("question", PrimitiveTypes.String);
            req.SetResponseSchema(schema);
            var res = await chatAgent.RequestJsonDocAsync(req);
            Question = res.RootElement.GetProperty("question").GetString();
        });
}