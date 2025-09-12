using YamlDotNet.Serialization;

namespace CX.Engine.HelmTemplates.Yaml;

public class YamlMap
{
    private readonly Dictionary<string, object> _map = new();
    
    public void Add(string key, string value)
    {
        var keys = key.Split('.');
        Add(keys, value);
    }

    public void Add(string[] keys, string value)
    {
        if (keys.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty", nameof(keys));
        }

        if (keys.Length == 1)
        {
            _map[keys[0]] = value;
            return;
        }

        var current = _map;
        for (var i = 0; i < keys.Length - 1; i++)
        {
            var containsKey = current.TryGetValue(keys[i], out var curVal);
            
            if (!containsKey)
                current[keys[i]] = curVal = new Dictionary<string, object>();
            
            if (curVal is Dictionary<string, object> dict) 
                current = dict;
            else
                throw new InvalidOperationException($"Key {keys[i]} is not a dictionary");
        }

        current[keys[^1]] = value;
    }

    public string Write() => new SerializerBuilder().Build().Serialize(_map);
}