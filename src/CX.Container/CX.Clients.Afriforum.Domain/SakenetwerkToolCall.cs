using System.Text.Json.Serialization;
using CX.Engine.Common.JsonSchemas;

namespace CX.Clients.Afriforum.Domain;

[JsonDerivedType(typeof(ListCities), typeDiscriminator: "List All Cities")]
[JsonDerivedType(typeof(CleanCities), typeDiscriminator: "Clean Cities")]
[JsonDerivedType(typeof(CleanProvinces), typeDiscriminator: "Clean Provinces")]
[JsonDerivedType(typeof(ExpandCategories), typeDiscriminator: "Expand Categories")]
[JsonDerivedType(typeof(ExpandTags), typeDiscriminator: "Expand Tags")]
[JsonDerivedType(typeof(NoTool), typeDiscriminator: "No tool")]
[Semantic]
public class SakenetwerkToolCall
{
}

public class NoTool : SakenetwerkToolCall
{
}

public class ListCities : SakenetwerkToolCall
{
    public string FilterRegex { get; set; }
}

public class CleanCities : SakenetwerkToolCall
{
}

public class CleanProvinces : SakenetwerkToolCall
{
}

public class ExpandCategories : SakenetwerkToolCall
{
}

public class ExpandTags : SakenetwerkToolCall
{
}