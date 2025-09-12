namespace CX.Engine.HelmTemplates;

public class RawGoExpression : GoExpression
{
    public string RawValue;
    
    public override string Emit() => "{{ " + RawValue + " }}";
    
    public RawGoExpression(string rawValue)
    {
        RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
    }

    public QuoteExpression Quote() => new(this);
}