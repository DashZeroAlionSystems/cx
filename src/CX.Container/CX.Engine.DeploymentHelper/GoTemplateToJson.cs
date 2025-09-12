using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CX.Engine.DeploymentHelper;

public class GoTemplateToJson
{
    private int _nextPlaceholderIdx;
    private readonly Dictionary<string, string> PlaceHolders = new();

    public string ReplaceGoTemplatesWithPlaceholders(string content)
    {
        return new Regex(@"{{\s*([^}]*)\s*}}").Replace(content, match =>
        {
            var placeholderId = $"GOTEMPLATE_{_nextPlaceholderIdx++}";
            PlaceHolders[placeholderId] = match.Value;
            return '"' + placeholderId + '"';
        });
    }

    public string ReplacePlaceholdersWithGoTemplates(string content)
    {
        foreach (var kvp in PlaceHolders)
            content = content.Replace('"' + kvp.Key + '"', kvp.Value);

        return content;
    }

    public JsonObject Load(string file)
    {
        var allText = File.ReadAllText(file);
        allText = ReplaceGoTemplatesWithPlaceholders(allText);
        return JsonSerializer.Deserialize<JsonObject>(allText, new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        })!;
    }

    public void Save(string path, JsonObject doc)
    {
        var allText = JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        allText = ReplacePlaceholdersWithGoTemplates(allText);
        File.WriteAllText(path, allText);
    }
}