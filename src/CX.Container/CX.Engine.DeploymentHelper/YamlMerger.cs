using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CX.Engine.DeploymentHelper;

public static class YamlMerger
{
    public static void Merge(string outPath, string inPath, string sourcePath)
    {
        // Read and deserialize the YAML file
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) // see height_in_inches in sample yml
            .Build();

        var target = deserializer.Deserialize<dynamic>(File.ReadAllText(inPath));
        var source = deserializer.Deserialize<dynamic>(File.ReadAllText(sourcePath));
        
        target["Config"]["AzureAITranslator"] = source["Config"]["AzureAITranslator"];
        target["Config"]["AzureContentSafety"] = source["Config"]["AzureContentSafety"];
        ((IDictionary<object, object>)target["Config"]).Remove("ContextAi"); 
        target["Config"]["ContextAI"] = source["Config"]["ContextAI"];
        target["Config"]["Langfuse"] = source["Config"]["Langfuse"];
        target["Config"]["Pinecone"] = source["Config"]["Pinecone"];
        target["Config"]["Attachments"] = source["Config"]["Attachments"];
        target["Config"]["Gpt4Vision"] = source["Config"]["Gpt4Vision"];
        ((IDictionary<object, object>)target["Config"]).Remove("LineSpliiter"); 
        target["Config"]["LineSplitter"] = source["Config"]["LineSplitter"];
        target["Config"]["Walter1Assistant"] = source["Config"]["Walter1Assistant"];
        target["Config"]["VectorLinkImporter"] = source["Config"]["VectorLinkImporter"];
        target["Config"]["StructuredData"] = source["Config"]["StructuredData"];
        target["Config"]["Weelee"] = source["Config"]["Weelee"];

        // Serialize the modified YAML object back to a string
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        
        // Save the modified YAML content back to the file
        File.WriteAllText(outPath, serializer.Serialize(target));
    }
}