namespace CX.Engine.Common;

public class ConfigValidationException : Exception
{
    public readonly string Path;
    public readonly string PropertyName;
    
    public ConfigValidationException(string path, string propertyName, string message) : base(message)
    {
        Path = path;
        PropertyName = propertyName;
    }
}