using System.Text.Json.Serialization.Metadata;

namespace CX.Engine.Common.Json;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class UseJsonTypeResolver : Attribute
{
    public Type ResolverType { get; }
    
    public UseJsonTypeResolver(Type resolverType)
    {
        if (!typeof(IJsonTypeInfoResolver).IsAssignableFrom(resolverType))
        {
            throw new ArgumentException($"{resolverType} must be a subclass of JsonConverter.", nameof(resolverType));
        }
        
        ResolverType = resolverType;
    }
}