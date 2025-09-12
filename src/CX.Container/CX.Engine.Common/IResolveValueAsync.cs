namespace CX.Engine.Common;

public interface IResolveValueAsync
{
    Task<object> ResolveValueAsync(string key, bool optional = false);
}