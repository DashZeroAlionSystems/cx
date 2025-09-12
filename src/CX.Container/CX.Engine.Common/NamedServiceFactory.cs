using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common;

public interface INamedServiceFactory<out T>
{
    public T GetService(string name, bool optional);
}

public class NamedServiceFactory<T> : INamedServiceFactory<T>
{
    private readonly Dictionary<string, T> _services = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Func<IServiceProvider, IConfiguration, string, bool, T> _newFunc;
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _configuration;
    private readonly bool _transient;
    private readonly bool _store;

    public NamedServiceFactory(IServiceProvider sp, IConfiguration config, Func<IServiceProvider, IConfiguration, string, bool, T> newFunc, bool transient,
        bool store)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _configuration = config ?? throw new ArgumentNullException(nameof(config));
        _newFunc = newFunc ?? throw new ArgumentNullException(nameof(newFunc));
        _transient = transient;
        _store = store;
    }

    public T GetService(string name, bool optional)
    {
        if (_transient)
            return _newFunc(_sp, _configuration, name, optional);

        lock (_services)
        {
            if (_services.TryGetValue(name, out var service) && _store)
                return service;

            var svc = _newFunc(_sp, _configuration, name, optional);

            if (svc != null)
                _services[name] = svc;

            return svc;
        }
    }
}

public static class NamedServiceFactoryExt
{
    public static void AddNamedSingletons<T>(this IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, IConfiguration, string, bool, T> newFunc, bool store = true, Action<IServiceProvider> init = null)
    {
        services.AddSingleton<INamedServiceFactory<T>>(sp =>
        {
            init?.Invoke(sp);
            return new NamedServiceFactory<T>(sp, configuration, newFunc, transient: false, store);
        });
    }

    public static void AddNamedTransients<T>(this IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, IConfiguration, string, bool, T> newFunc, bool store = true, Action<IServiceProvider> init = null)
    {
        services.AddSingleton<INamedServiceFactory<T>>(sp =>
        {
            init?.Invoke(sp);
            return new NamedServiceFactory<T>(sp, configuration, newFunc, transient: true, store);
        });
    }

    public static T GetRequiredNamedService<T>(this IServiceProvider sp, string name) => sp.GetRequiredService<INamedServiceFactory<T>>().GetService(name, false);

    public static T GetNamedService<T>(this IServiceProvider sp, string name, bool optional = true) where T : class
    {
        if (!optional)
            return sp.GetRequiredService<INamedServiceFactory<T>>().GetService(name, false);
        else
            return sp.GetService<INamedServiceFactory<T>>()?.GetService(name, true);
    }

    public static void GetRequiredNamedService<T>(this IServiceProvider sp, out T svc, string name, IConfigurationSection section,
        [CallerArgumentExpression(nameof(svc))]
        string propertyName = null) where T : class
    {
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));

        svc = sp.GetNamedService<T>(name);
        section.ThrowIfNamedServiceNotFound(svc, name, propertyName);
    }

    public static void GetRequiredNamedService<TBase, TInherit>(this IServiceProvider sp, out TInherit svc, string name, IConfigurationSection section,
        [CallerArgumentExpression(nameof(svc))]
        string propertyName = null)
        where TBase : class
        where TInherit : class, TBase
    {
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));

        var baseSvc = sp.GetNamedService<TBase>(name);
        if (baseSvc != null)
        {
            if (baseSvc is not TInherit actual)
                throw new InvalidOperationException(
                    $"{propertyName}: Service '{name}' is not of type {typeof(TInherit).Name} but of type {baseSvc.GetType().Name} for {section.Path}");
            else
                svc = actual;
        }
        else
            svc = default;

        section.ThrowIfNamedServiceNotFound(svc, name, propertyName);
    }
}