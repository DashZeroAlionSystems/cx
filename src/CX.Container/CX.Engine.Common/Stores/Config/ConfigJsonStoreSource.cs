using System.Diagnostics;
using System.Text.Json.Nodes;
using CX.Engine.Common.Json;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.Stores.Json;

public class ConfigJsonStoreSource
{
    public string Section;
    public string TableName;
    
    public ConfigJsonStoreSource(string section, string tableName)
    {
        Section = section;
        TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
    }

    public async Task MergeAsync(IJsonStore store, JsonObject root, ILogger logger)
    {
        var section = Section == null ? root.AsObject() : root[Section]?.AsObject();
        if (section == null)
            root.Add(Section, section = new());
        
        foreach (var row in await store.GetAllAsync())
        {
            if (row.Value == null)
                continue;

            var s = JsonRawStringProcessor.NormalizeTripleQuotedStrings(row.Value);
            

            try
            {
                section[row.Key] = JsonNode.Parse(s);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error parsing JSON for {TableName} {row.Key}: {s}");
            }
        }
    }

    public override string ToString() => TableName;
}