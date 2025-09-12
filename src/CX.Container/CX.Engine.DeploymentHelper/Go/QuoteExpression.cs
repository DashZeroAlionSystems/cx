namespace CX.Engine.HelmTemplates;

public class QuoteExpression : GoExpression
{
    public readonly RawGoExpression Inner;

    public override string Emit() => "{{ " + Inner.RawValue + " | quote }}";
    
    public QuoteExpression(RawGoExpression inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }
}