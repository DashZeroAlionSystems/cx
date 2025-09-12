namespace CX.Engine.HelmTemplates.Yaml;

public static class YamlExt
{
    /// <summary>
    /// Adds a hash before and curly braces around a string.
    /// </summary>
    public static string Hashbrace(this string s)
    {
        return "#{" + s + "}";
    }
    
}