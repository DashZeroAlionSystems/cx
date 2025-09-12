using CX.Engine.Assistants.AssessmentBuilder;
using CX.Engine.Assistants.AssessmentBuilder.Xml;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Xml;
using JetBrains.Annotations;
using static CX.Engine.Common.Xml.CxmlCommon;

namespace CX.Engine.Assistants.AssessmentBuilder.Xml;

public static class AssessmentCxml
{
    public static async Task<AssessmentPaper> ParseAsync(string input, CxmlScope scope = null)
    {
        [CxmlFactory]
        AssessmentPaper Paper(CxmlScope scope)
        {
            var res = new AssessmentPaper();
            scope.Context["paper"] = res;
            return res;
        }

        [CxmlFactory]
        AssessmentSection Section() => new();

        [CxmlFactory]
        AssessmentPassage Passage() => new();

        [CxmlFactory]
        AssessmentInstructions Instructions() => new();
        
        [CxmlFactory]
        MultipleChoiceQuestion MultipleChoiceQuestion() => new();
        
        [CxmlFactory]
        SimpleQuestion SimpleQuestion() => new();

        Delegate[] actions = [Paper, Section, Passage, Instructions, Foreach, Paragraph, Header1, Header2, Header3, MultipleChoiceQuestion, SimpleQuestion, OrderedList, ListItem, SpaceNode, Include, ScopeNode, PromptSection];

        if (scope != null)
        {
            if (scope.Preparation.MethodNames.Count > 0)
                throw new InvalidOperationException("CxmlScope should not have any methods");
            
            scope.SetActions(actions);
        }
        else 
            scope ??= new(actions);
        
        return await Cxml.ParseToObjectAsync<AssessmentPaper>(input, scope);
    }

    public static async Task<(string Name, string Content)?> EvalStringAsync(
        [LanguageInjection(InjectedLanguage.HTML)]
        string input, CxmlScope scope = null)
    {
        scope ??= new();
        TextRenderContext.Current.Scope = scope;
        
        var doc = await ParseAsync(input, scope);

        if (doc == null)
            return null;

        scope.Context["paper"] = doc;
        scope.Context["nodes"] = new NodeSelector(doc);

        await Cxml.PerformComputeStagesAsync(doc, scope);

        var res = await doc.RenderToStringAsync(scope, smartFormat: true);
        return (doc.Subject, res);
    }
}