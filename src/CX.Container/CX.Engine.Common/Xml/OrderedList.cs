using CX.Engine.Common.Numbering;
using CX.Engine.Common.Rendering;

namespace CX.Engine.Common.Xml;

public class OrderedList : ICxmlAddChild, IRenderToText, ICxmlChildren, ICxmlHasParentProp
{
    public readonly List<ListItem> Items = [];
    public string Type;
    public string Delimiter;
    public string Prefix;
    public string Suffix;
    public string Start;
    public bool Nest = true;
    public IEnumerable<object> CxmlChildren() => Items;
    public object Parent { get; set; }

    public async Task AddChildAsync(object o)
    {
        if (o is ListItem li)
            Items.Add(li);
    }

    public async Task RenderToTextAsync()
    {
        var ctx = TextRenderContext.InheritOrNew();
        
        var setup = (Nest ? ctx.OlSetup?.Nest() : null) ?? new OrderedListSetup();

        if (Type == "1")
            setup.Sequence = new NumericOrderedListSequence();
        else if (Type == "a")
            setup.Sequence = new AlphabeticOrderedListSequence() { Uppercase = false };
        else if (Type == "A")
            setup.Sequence = new AlphabeticOrderedListSequence() { Uppercase = true };
        else if (Type == "i")
            setup.Sequence = new RomanNumeralOrderedListSequence() { Uppercase = false };
        else if (Type == "I")
            setup.Sequence = new RomanNumeralOrderedListSequence() { Uppercase = true };

        if (Start != null)
        {
            if (setup.Sequence is NumericOrderedListSequence nol && int.TryParse(Start, out var i))
                nol.CurrentPos = i - 1;
        }


        if (Delimiter != null)
            setup.Delimiter = Delimiter;

        if (Prefix != null)
            setup.Prefix = Prefix;

        if (Suffix != null)
            setup.Suffix = Suffix;
        
        ctx.OlSetup = setup;

        var sb = ctx.Sb;
        TextRenderContext.EnsureStartsOnNewLine();
      
        foreach (var item in Items)
        {
            sb.Append(setup.Next());
            await item.RenderToTextAsync();
            TextRenderContext.EnsureStartsOnNewLine();
        }
    }
}