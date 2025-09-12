using CX.Engine.ChatAgents;
using CX.Engine.Common;
using CX.Engine.Common.JsonSchemas;
using Cx.Engine.Common.PromptBuilders;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Tracing;
using CX.Engine.Common.Xml;

namespace CX.Engine.Assistants.AssessmentBuilder.Xml;

public class AssessmentPassage : BaseNode, IRenderToText, ICxmlComputeNode, ICxmlId, ICxmlHasParentProp, ICxmlHasPromptSectionsProp
{
    public object Parent { get; set; }
    public List<object> DependsOn { get; set; }

    public List<PromptSectionNode> PromptSections { get; set; } = [];

    public string Name;
    public string Title;
    public string Text;
    public string Prompt;
    public bool GenerateTitle = true;
    public string Id { get; set; }
    public const string TypeId = "assessment-passage";
    public string FullId => $"{TypeId}.{Id}";
    public string SchemaName => TypeId.ToValidOpenAISchemaName();
    
    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.Current;
        var sb = ctx.Sb;
        
        TextRenderContext.EnsureStartsOnNewLine(1);

        if (Name != null)
        {
            sb.Append(Name);
            TextRenderContext.EnsureStartsOnNewLine(1);
        }
        
        if (Title != null)
        {
            sb.Append(Title);
            TextRenderContext.EnsureStartsOnNewLine(1);
        }

        if (Text != null)
            sb.Append(Text.RemoveCommonIndentation());

        TextRenderContext.EnsureStartsOnNewLine(1);
    }

    public async Task InternalComputeAsync(CxmlScope scope)
    {
        if (string.IsNullOrWhiteSpace(Prompt))
            return;

        var utils = await GetUtilsAsync(scope);
        var chatAgent = utils.Snapshot.ChatAgent;

        await CXTrace.Current.SpanFor(FullId, new { SchemaName = SchemaName, Prompt = Prompt }).ExecuteAsync(async _ =>
        {
            var pb = new PromptBuilder();
            var sections = this.ResolvePromptSections();            
            await pb.AddAsync(sections, scope);

            pb.Add(Prompt, 10_000);
            var prompt = pb.GetPrompt();

            var req = chatAgent.GetRequest(prompt);
            req.StringContext.Add(await utils.ContextForPromptAsync(prompt)); 
            var schema = chatAgent.GetSchema(SchemaName);
            schema.Object.AddProperty("reasoning", PrimitiveTypes.String);
            if (GenerateTitle)
                schema.Object.AddProperty("title", PrimitiveTypes.String);
            schema.Object.AddProperty("text", PrimitiveTypes.String);
            req.SetResponseSchema(schema);
            var res = await chatAgent.RequestJsonDocAsync(req);
            if (GenerateTitle)
                Title = res.RootElement.GetProperty("title").GetString();
            Text = res.RootElement.GetProperty("text").GetString();
        });
    }
}