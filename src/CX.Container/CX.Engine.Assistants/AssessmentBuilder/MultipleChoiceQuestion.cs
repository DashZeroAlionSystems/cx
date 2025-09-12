using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using Cx.Engine.Common.PromptBuilders;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Xml;
using HtmlAgilityPack;

namespace CX.Engine.Assistants.AssessmentBuilder;

public class MultipleChoiceQuestion : BaseNode, IRenderToText, ICxmlId, ICxmlComputeNode, ICxmlHasParentProp, ICxmlHasPromptSectionsProp
{
    public object Parent { get; set; }
    public List<object> DependsOn { get; set; } = [];

    public List<PromptSectionNode> PromptSections { get; set; } = [];

    public string Question;
    
    [CxmlChildrenByName("choice")]
    public List<object> Choices;

    public int Marks;

    public string Id { get; set; }
    public HtmlNode Prompt;
    public const string TypeId = "multiple-choice-question";
    public string FullId => $"{TypeId}.{Id}";
    public string SchemaName => TypeId.ToValidOpenAISchemaName();
    
    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;
        sb.AppendLine(Question);
        var first = true;

        foreach (var choice in Choices)
        {
            if (!first)
                sb.AppendLine();
            
            first = false;
            await choice.RenderToTextContextAsync();
        }

        if (Marks > 0)
            sb.Append($" ({Marks})");
        
        sb.AppendLine();
    }

    public Task InternalComputeAsync(CxmlScope scope)
    {
        return CXTrace.Current.SpanFor(FullId, new { Prompt = Prompt?.InnerHtml?.Trim() }).ExecuteAsync(async _ =>
        {
            string prompt = null;

            scope = scope.Inherit();
            scope.Context["question"] = Question;
            scope.TopLevelNodeHandler = CxmlCommon.ContainerNode;

            if (Prompt != null)
                prompt = await Cxml.EvalStringAsync(Prompt, scope);

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
            schema.Object.AddProperty("question", PrimitiveTypes.String);
            schema.Object.AddProperty("choices", PrimitiveTypes.Array, itemType: PrimitiveTypes.String);
            req.SetResponseSchema(schema);
            var json = await chatAgent.RequestJsonDocAsync(req);
            Question = json.RootElement.GetProperty("question").GetString();
            Choices = json.RootElement.GetProperty("choices").EnumerateArray().Select(e => (object)e.GetString()).ToList();
        });
    }
}