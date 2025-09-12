using CX.Engine.Assistants.AssessmentBuilder.Xml;
using CX.Engine.Common.Xml;

namespace CX.Engine.Assistants.AssessmentBuilder;

public class BaseNode : ICxmlHasParentProp
{
    public object Parent { get; set; }
    public async Task<BaseNodeUtils> GetUtilsAsync(CxmlScope scope)
    {
        var ss = await scope.ResolveValueAsync<AssessmentAssistant.Snapshot>("Snapshot");
        if (ss == null)
            throw new InvalidOperationException("Missing scope.Snapshot");

        var paper = this.GetAncestor<AssessmentPaper>(true);
        if (paper == null)
            throw new InvalidOperationException("Missing AssessmentPaper ancestor");

        return new(ss, paper);
    }
}