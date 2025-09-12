using System.Dynamic;
using System.Text;
using CX.Engine.Common;
using CX.Engine.Common.Formatting;
using SmartFormat;

namespace Cx.Engine.Common.PromptBuilders;

public class StructureDescriptionSection : PromptSection
{
    public bool ApplySmartFormat = true;

    public string Header = "Respond with a JSON structure having the properties:";
    public Dictionary<string, Field> Fields = new();

    public class Field
    {
        public string Description;
        public Dictionary<string, object> Context;
        
        public Field(string description, Dictionary<string, object> context = null)
        {
            Description = description;
            Context = context;
        }
    }

    public override string GetContent(ExpandoObject context)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(Header))
            sb.AppendLine(ApplySmartFormat ? Smart.Format(Header, context) : Header);

        foreach (var kvp in Fields)
        {
            var description = kvp.Value.Description;

            if (ApplySmartFormat)
            {
                if ((kvp.Value.Context?.Count ?? 0) > 0)
                {
                    var parContext = context.Clone();
                    parContext.Merge(kvp.Value.Context);
                    if (description != null)
                        description = CxSmart.Format(description, parContext);
                }
                else
                {
                    description = CxSmart.Format(description, context);
                }
            }

            if (string.IsNullOrWhiteSpace(description))
                sb.AppendLine($"  - {kvp.Key}");
            else
                sb.AppendLine($"  - {kvp.Key}: {description}");
        }

        var res = sb.ToString();
        return res;
    }
}