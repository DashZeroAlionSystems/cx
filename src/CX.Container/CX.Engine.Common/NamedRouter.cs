using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common;

public class NamedRouter<T> : Dictionary<string, Func<string, IServiceProvider, IConfiguration, bool, T>>
{
    public readonly string KeyDescription;
    
    public NamedRouter(string keyDescription) : base(StringComparer.InvariantCultureIgnoreCase) 
    {
        KeyDescription = keyDescription ?? throw new ArgumentNullException(nameof(keyDescription));
    }

    public void Init(IServiceProvider sp)
    {
        foreach (var init in sp.GetServices<NamedRouterInit<T>>())
            init(this, sp);
    }

    public T Route(IServiceProvider sp, IConfiguration config, string name, bool optional)
    {
        var (routeName, subName) = name.SplitAtFirst(".");

        if (TryGetValue(routeName, out var factory))
            return factory(subName, sp, config, optional);

        if (optional)
            return default;
        else
            throw new InvalidOperationException($"Unknown {KeyDescription}: {routeName}");
    }
}

public static class NamedRouterExt
{
    public static NamedRouter<T> AddNamedTransientRouter<T>(this IServiceCollection services, IConfiguration configuration, string keyDescription)
    {
        var router = new NamedRouter<T>(keyDescription);
        services.AddSingleton(router);
        services.AddNamedTransients(configuration, router.Route, init: router.Init);
        return router;
    }
    
    public static NamedRouter<T> AddNamedSingletonRouter<T>(this IServiceCollection services, IConfiguration configuration, string keyDescription)
    {
        var router = new NamedRouter<T>(keyDescription);
        services.AddSingleton(router);
        services.AddNamedSingletons(configuration, router.Route, init: router.Init);
        return router;
    }

    public static void AddRoute<T>(this IServiceProvider sp, string routeName, Func<string, IServiceProvider, IConfiguration, bool, T> factory) 
        => sp.GetRequiredService<NamedRouter<T>>()[routeName] = factory;

    public static void AddRoute<T>(this IServiceCollection sc, string routeName, Func<string, IServiceProvider, bool, T> factory) => AddRoute(sc, routeName, (name, sp, _, optional) => factory(name, sp, optional));
    public static void AddRoute<T>(this IServiceCollection sc, string routeName, Func<string, IServiceProvider, IConfiguration, bool, T> factory)
    {
        sc.AddSingleton<NamedRouterInit<T>>(_ => (router, _) => router[routeName] = factory);
    }
}