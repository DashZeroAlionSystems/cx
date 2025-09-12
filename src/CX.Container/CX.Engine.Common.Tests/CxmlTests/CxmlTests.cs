using System.Dynamic;
using System.Text;
using CX.Engine.Common.Formatting;
using CX.Engine.Common.Rendering;
using CX.Engine.Common.Xml;
using HtmlAgilityPack;
using JetBrains.Annotations;
using static CX.Engine.Common.MiscHelpers;
using static CX.Engine.Common.Xml.CxmlCommon;
using static CX.Engine.Common.Xml.CxmlEntities;

namespace CX.Engine.Common.Tests;

public class CxmlTests
{
    [Fact]
    public async Task CxmlParseTests()
    {
        string HiWorld() => "Hi, world!";

        [CxmlAction("HiWorldAsync")]
        Task<string> HiWorld2Async() => Task.FromResult("Hi, world! (async)");

        var scope = new CxmlScope(HiWorld, HiWorld2Async);

        {
            var res = await Cxml.EvalStringAsync("""
                                                   Line 1<br/>
                                                   <hi-world></hi-world><br/>
                                                   Line 3
                                                   """, scope);
            Assert.Equal("""
                         Line 1
                         Hi, world!
                         Line 3
                         """, res);
        }

        {
            var res = await Cxml.EvalStringAsync("""
                                                   Line 1<br/>
                                                   <hi-world /><br/>
                                                   Line 3
                                                   """, scope);
            Assert.Equal("""
                         Line 1
                         Hi, world!
                         Line 3
                         """, res);
        }

        {
            var res = await Cxml.EvalStringAsync("<HiWorldAsync />", scope);
            Assert.Equal("Hi, world! (async)", res);
        }
    }
    
        [Fact]
    public async Task TestWithArguments()
    {
        string Echo([CxmlContent]string content, bool upper = true) => upper ? content?.ToUpperInvariant() : content;

        var scope = new CxmlScope(Echo);

        {
            var res = await Cxml.EvalStringAsync("<Echo>Hi, world!</Echo>", scope);
            Assert.Equal("HI, WORLD!", res);
        }

        {
            var res = await Cxml.EvalStringAsync("<Echo upper=false>Hi, world!</Echo>", scope);
            Assert.Equal("Hi, world!", res);
        }
        
        {
            var res = await Cxml.EvalStringAsync("<Echo upper=false content='Hi, world!' />", scope);
            Assert.Equal("Hi, world!", res);
        }

        {
            var res = await Cxml.EvalStringAsync("<Echo upper=false content='Hi, world!'>", scope);
            Assert.Equal("Hi, world!", res);
        }
        
        {
            var res = await Cxml.EvalStringAsync("<Echo upper=false>Hi, world!</>", scope);
            Assert.Equal("Hi, world!", res);
        }
    }

    [Fact]
    public async Task TestComboParse()
    {
        {
            int Add(int left, int right) => left + right;
            object IronPython([CxmlContent]string script) => IronPythonExecutor.ExecuteScriptAsync(script);
            
            var res = await Cxml.EvalStringAsync($$"""
                                                    Exam Paper
                                                    
                                                    Marks:{{SpaceEntity}}<add left={x} right={y} /><br/>
                                                    Time: 80 minutes
                                                    <br/><br/>
                                                    <iron-python>
                                                    'SECTION' + ' ' + 'A'
                                                    </>
                                                    """, new { x = 1, y = 2 }, Add, IronPython);

            Assert.Equal("""
                         Exam Paper
                         
                         Marks: 3
                         Time: 80 minutes
                         
                         SECTION A
                         """, res);
        }
    }


    [Fact]
    public async Task EvalStringTests()
    {
        Task<string> Echo(string content) => Task.FromResult(content);
        var scope = new CxmlScope(new { x = Task.FromResult(1) }, Echo);
        var res = await Cxml.EvalStringAsync("<echo content={x} />", scope);
        Assert.Equal("1", res);
        res = await Cxml.EvalStringAsync("<echo content='{x}' />", scope);
        Assert.Equal("1", res);
    }


    [Fact]
    public async Task RawInjectTests()
    {
        Task<string> Echo(string content) => Task.FromResult(content);
        var scope = new CxmlScope(new { x = Task.FromResult(1) }, Echo);
        var res = await Cxml.EvalStringAsync("<echo content='raw:{x}' />", scope);
        Assert.Equal("{x}", res);
    }
    
    public class SumOperation : ICxmlAddChild, IRenderToText
    {
        private readonly List<int> _children = [];
        
        public async Task RenderToTextAsync()
        {
            TextRenderContext.Current.Sb.Append(_children.Sum().ToString("#,##0"));
        }

        public async Task AddChildAsync(object o)
        {
            if (o is int i)
                _children.Add(i);
        }
    }

    [Fact]
    public async Task ChildrenTestsAsync()
    {
        int Add(int left, int right) => left + right;
        SumOperation Sum() => new();
        var scope = new CxmlScope(Add, Sum);
        var res = await Cxml.EvalStringAsync("<sum><add left=1 right=2 /><add left=3 right=4 /></sum>", scope);
        Assert.Equal("10", res);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PropTestClass 
    {
        [CxmlRequired]
        public string PropA;
        public string PropB { get; set; }
        public override string ToString() => PropA + " " + PropB;
    }

    [Fact]
    public async Task PropTestAsync()
    {
        [CxmlFactory] PropTestClass PropTest() => new();
        var scope = new CxmlScope(PropTest);
        {
            var res = await Cxml.EvalStringAsync("<prop-test prop-a='A' prop-b='B' />", scope);
            Assert.Equal("A B", res);
        }

        {
            var res = await Cxml.EvalStringAsync("<prop-test prop-a='A' />", scope);
            Assert.Equal("A ", res);
        }
    }

    [Fact]
    public async Task PropRequiredTestAsync()
    {
        [CxmlFactory] PropTestClass PropTest() => new();
        var scope = new CxmlScope(PropTest);
        await Assert.ThrowsAsync<CxmlException>(async () => await Cxml.EvalStringAsync("<prop-test PropB='B' />", scope));
    }

    [Fact]
    public async Task DictionaryFactoryTestAsync()
    {
        [CxmlFactory] dynamic MyExpando() => new ExpandoObject();
        var scope = new CxmlScope(MyExpando);
        {
            var res = await Cxml.ParseToObjectAsync<ExpandoObject>("<my-expando PropA='A' PropB='B' />", scope);
            Assert.IsType<ExpandoObject>(res);
            Assert.Equal("A", ((IDictionary<string, object>)res)["propa"]);
            Assert.Equal("B", ((IDictionary<string, object>)res)["propb"]);
        }
    }
    
    public class ChildrenContainer : IRenderToText
    {
        public string Text;
        public List<object> Children;
        
        public async Task RenderToTextAsync()
        {
            Children ??= [];
            if (Children?.Count == 0)
                TextRenderContext.Current.Sb.Append(Text);
            if (Children?.Count > 0)
                await Children.RenderToTextContextAsync();
        }
    }

    [Fact]
    public async Task ChildrenAttrTestAsyncs()
    {
        ChildrenContainer Container([CxmlContent]string text, [CxmlChildren] List<object> children) => new() { Text = text, Children = children ?? [] };
        ChildrenContainer Child([CxmlContent]string text, [CxmlChildren] List<object> children) => new() { Text = text, Children = children ?? [] };
        Task<object> IronPythonAsync([CxmlContent]string input) => IronPythonExecutor.ExecuteScriptAsync(input?.Trim());

        var scope = new CxmlScope(Container, Child, IronPythonAsync);
        {
            var res = await Cxml.ParseToObjectAsync<ChildrenContainer>($"""
                                                                                 <container>
                                                                                   <child>hi there!</child>
                                                                                   <child>{SpaceEntity}and here!{SpaceEntity}</child>
                                                                                   <iron-python>
                                                                                   5 + 5
                                                                                   </>
                                                                                 </container>
                                                                                 """, scope);
            Assert.Equal(3, res.Children.Count);
            Assert.Equal("hi there!", ((ChildrenContainer)res.Children[0]).Text);
            Assert.Equal(" and here! ", ((ChildrenContainer)res.Children[1]).Text);
            Assert.Equal(10, res.Children[2]);

            var sb = new StringBuilder();
            TextRenderContext.InheritOrNew(sb);
            await res.RenderToTextAsync();
            var sRes = sb.ToString().Replace(NBSP, ' ');
            Assert.Equal("hi there! and here! 10", sRes);
        }
    }

    [Fact]
    public async Task TestScopesAsync()
    {
        static async Task<string> IncAsync(CxmlScope scope, string var, bool global=false)
        {
            int val;
            var ctx = global ? scope.Root : scope.Parent;
            ctx[var] = val = await ctx.ResolveValueOrDefaultAsync<int>(var) + 1;
            return $"{var}={val}\r\n";
        }

        static async Task<string> Scope([CxmlChildren(All = true, UnknownAsStrings = true)] List<object> children)
        {
            var sb = new StringBuilder();
            TextRenderContext.InheritOrNew(sb);
            await children.RenderToTextContextAsync();
            return sb.ToString();
        }

        var scope = new CxmlScope(IncAsync, Scope);
        var res = await Cxml.EvalStringAsync("""
                                         <scope>
                                             <inc var='i' />
                                             <inc var='i' />
                                             <scope>
                                               <inc var='i' />
                                               <inc var='j' global=true />
                                             </scope>
                                             <inc var='i' />
                                             <inc var='j' />
                                         </scope>
                                         """, scope);
        Assert.Equal("""
                     i=1
                     i=2
                     i=3
                     j=1
                     i=3
                     j=2
                     """, res.Trim());
    }

    [Fact]
    public async Task DefaultOrderedListTest()
    {
        var scope = new CxmlScope(OrderedList, ListItem);
        var res = await Cxml.EvalStringAsync("""
                                         <ol>
                                           <li>Item 1</li>
                                           <li>Item 2</li>
                                         </ol>
                                         """, scope);
        Assert.Equal("""
                     1. Item 1
                     2. Item 2
                     
                     """, res);
    }

    [Fact]
    public async Task ParagraphAndHeaderTests()
    {
        var scope = new CxmlScope(Paragraph, Header1, Header2, Header3);
        var res = await Cxml.EvalStringAsync("""
                                         <p>
                                         <h1>Hi there!</h1>
                                         <p>Content.</p>
                                         </p>
                                         """, scope);
        
        Assert.Equal("""
                     Hi there!
                     
                     Content.
                     
                     """, res);
    }

    [Fact]
    public async Task NestedOrderedListTest()
    {
        var scope = new CxmlScope(OrderedList, ListItem);
        var res = await Cxml.EvalStringAsync("""
                                         <ol>
                                           <li>Teas<br/>
                                           <ol type="a">
                                             <li>Rooibos</li>
                                             <li>Green</li>
                                           </ol>
                                           </li>
                                           <li>Coffee</li>
                                         </ol>
                                         """, scope);
        Assert.Equal("""
                     1. Teas
                     1.a. Rooibos
                     1.b. Green
                     2. Coffee
                     
                     """, res);
    }

    public class HtmlNodeFieldClass
    {
        public HtmlNode Template;
        public string Value;
    }

    [Fact]
    public async Task LateBindingHtmlTest()
    {
        [CxmlFactory]
        HtmlNodeFieldClass TestNode() => new();

        object IronPython([CxmlContent]string script) => IronPythonExecutor.ExecuteScriptAsync(script);

        {
            var scope = new CxmlScope(TestNode, Paragraph, IronPython);

            var res = await Cxml.ParseToObjectAsync<HtmlNodeFieldClass>(
                "<test-node><template><p><iron-python>'Hi' + ' ' + 'there'</iron-python>{exclam}</p></template></test-node>", scope);
            scope.Context["exclam"] = "!";

            var output = await Cxml.EvalStringAsync(res.Template, scope);
            Assert.Equal("Hi there!\r\n", output);
        }
        
        HtmlNodeFieldClass TestNode2(HtmlNode prompt) => new() { Template = prompt };

        {
            var scope = new CxmlScope(TestNode2, Paragraph, IronPython);

            var res = await Cxml.ParseToObjectAsync<HtmlNodeFieldClass>(
                "<test-node2><prompt><p><iron-python>'Hi' + ' ' + 'there'</iron-python>{exclam}</p></prompt></test-node>", scope);
            scope.Context["exclam"] = "!";

            var output = await Cxml.EvalStringAsync(res.Template, scope);
            Assert.Equal("Hi there!\r\n", output);
        }
    }

    public class ComputeTestClass : ICxmlComputeNode, IRenderToText, ICxmlId
    {
        public List<object> DependsOn { get; set; } = [];
        public int Input;
        public int Output;
        public string Id { get; set; }

        public async Task InternalComputeAsync(CxmlScope scope)
        {
            Output = Input;
            
            foreach (var dep in DependsOn)
                if (dep is ComputeTestClass ctc)
                    Output += ctc.Output;
        }

        public async Task RenderToTextAsync()
        {
            var ctx = TextRenderContext.Current;
            var sb = ctx.Sb;
            sb.AppendLine($"{Output}");
        }
    }

    [Fact]
    public async Task DependencyTest()
    {
        [CxmlFactory]
        ComputeTestClass Test() => new();

        var scope = new CxmlScope(Test);
        var res = await Cxml.EvalStringAsync("""
                                             <test input=1 id='a' />
                                             <test input=5 id='c'>
                                               <depends-on>nodes.a</depends-on>
                                               <depends-on>nodes.b</depends-on>
                                             </test><test input=2 id='b' />
                                             """, scope);

        Assert.Equal("""
                     1
                     8
                     2
                     
                     """, res);
    }
}