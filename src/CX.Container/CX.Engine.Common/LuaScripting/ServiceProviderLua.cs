using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.LuaScripting;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ServiceProviderLua : ILuaCoreLibrary
{
    public readonly IServiceProvider Sp;

    public ServiceProviderLua([NotNull] IServiceProvider sp)
    {
        Sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public void Setup(LuaInstance instance)
    {
        instance.Script.Globals["ServiceProvider"] = this;
    }
    
    public T GetRequiredNamedService<T>(string name) => Sp.GetRequiredService<INamedServiceFactory<T>>().GetService(name, false);
    
    public LuaLogger GetLogger(string name) => LuaLogger.For(Sp.GetLogger(name));

    public object GetRequiredService(string type) => Sp.GetRequiredService(LuaCore.ResolveTypeName(type, required: true));

    public object GetRequiredNamedService(string type, string name)
    {
        var serviceType = LuaCore.ResolveTypeName(type, required: true);

        // Get the generic method GetRequiredService for the INamedServiceFactory<T>
        var factoryType = typeof(INamedServiceFactory<>).MakeGenericType(serviceType);
        var factory = Sp.GetRequiredService(factoryType);

        // Get the GetService method on the factory
        var getServiceMethod = factoryType.GetMethod(nameof(INamedServiceFactory<object>.GetService));

        if (getServiceMethod == null)
        {
            throw new InvalidOperationException($"Could not find method GetService on {factoryType.Name}.");
        }

        // Invoke the method to get the named service
        return getServiceMethod.Invoke(factory, [name, false]);
    }

    public async void Kill(int seconds, int exitCode = -1)
    {
        await Task.Delay(1_000 * seconds);
        Environment.Exit(exitCode);
    }
}