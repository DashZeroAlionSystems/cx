using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CX.Engine.Common;
using CX.Engine.Common.Json;
using CX.Engine.Common.JsonSchemas;
using CX.Engine.Common.SqlKata;
using JetBrains.Annotations;

namespace CX.Engine.Assistants.QueryAssistants;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SqlServerReportParameter : IValidatable
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string DefaultValue { get; set; }
    public string LlmDescription { get; set; }
    public List<string> Choices { get; set; }
    public bool Nullable { get; set; }
    public bool IsSqlServerParameter { get; set; } = true;
    public bool IsArray { get; set; }
    public string FilterRule { get; set; }
    public SqlServerReportType ParsedType;
    public bool AllowMultiple { get; set; }
    public List<SqlKataFunctionType> Functions { get; set; } = [];
    public string FormatString { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"{nameof(Name)} is required.");
        
        if (string.IsNullOrWhiteSpace(Type))
            throw new InvalidOperationException($"{nameof(Type)} is required.");
        
        if (!Enum.TryParse(Type, out ParsedType))
            throw new InvalidOperationException($"{nameof(Type)} is invalid.");
    }
}