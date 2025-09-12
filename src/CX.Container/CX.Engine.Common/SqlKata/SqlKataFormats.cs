namespace CX.Engine.Common.SqlKata;

public class SqlKataFormats : Dictionary<string, string>
{
    private List<string> _keysToRemove = [];
    private List<KeyValuePair<string, string>> _keysToAdd = [];
    public void UpdateKey(string key, string newKey)
    {
        _keysToRemove.Add(key);
        _keysToAdd.Add(new KeyValuePair<string, string>(newKey, base[key]));
    }

    public void Update()
    {
        foreach (var keyToRemove in _keysToRemove)
            Remove(keyToRemove);
        
        foreach(var keyToAdd in _keysToAdd)
            Add(keyToAdd.Key, keyToAdd.Value);
        
        _keysToAdd.Clear();
        _keysToRemove.Clear();
    }
    
    public SqlKataFormats Clone()
    {
        var clone = new SqlKataFormats();
        clone.AddRange(this);
        return clone;
    }
}