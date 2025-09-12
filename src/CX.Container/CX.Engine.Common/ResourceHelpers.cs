namespace CX.Engine.Common;

public static class ResourceHelpers
{
    public static Stream Get<T>(string name) => typeof(T).Assembly.GetManifestResourceStream(name) ??
                                                  throw new InvalidOperationException(
                                                      $"Resource {name} not found in assembly {typeof(T).Assembly.FullName}");
    
    public static Stream GetResource(this object obj, string name)
    {
        if (!(obj is Type type))
            type = obj.GetType();
        
        return type.Assembly.GetManifestResourceStream(name) ??
               throw new InvalidOperationException(
                   $"Resource {name} not found in assembly {obj.GetType().Assembly.FullName}");
    }
}
