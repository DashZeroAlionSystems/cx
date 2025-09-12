using CX.Engine.Common;

namespace CX.Engine.HelmTemplates.Yaml;

public class YamlValue
{
    public string[] Keys;
    public string Value;
    public bool Quote;
    
    public GoExpression Exp() => Quote ? new RawGoExpression(".Values." + string.Join('.', Keys)).Quote() : new RawGoExpression(".Values." + string.Join('.', Keys));

    public YamlValue(YamlMap map, string[] keys, string value, bool quote)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        Value = value;
        Keys = keys;
        Quote = quote;
        map.Add(Keys, Value);
    }

    public YamlValue(YamlMap map, string key, string value, bool quote)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        
        key = key.RemoveLeading(".")!;
        Value = value;
        Keys = key.Split('.');
        Quote = quote;
        map.Add(Keys, Value);
    }
}