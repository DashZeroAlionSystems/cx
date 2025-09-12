using Microsoft.Extensions.Options;

namespace CX.Engine.Common;

public class ConfigureOptionsDelegate<T> : IConfigureOptions<T> where T : class
{
    public ConfigureOptionsDelegate(Action<T> configure)
    {
        OnConfigure = configure;
    }
    
    public Action<T> OnConfigure;
    
    public void Configure(T options)
    {
        OnConfigure?.Invoke(options);
    }
}