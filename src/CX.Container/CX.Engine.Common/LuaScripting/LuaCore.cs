using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop.RegistrationPolicies;

namespace CX.Engine.Common;

public class LuaCore : IDisposable
{
    private readonly IServiceProvider _sp;
    private LuaCoreOptions _options;
    private readonly IDisposable _optionsChangeMonitorDisposable;

    public LuaInstance GetLuaInstance()
    {
        var res = new LuaInstance(_sp.GetLogger<LuaInstance>());
        var opts = _options;
        UserData.RegistrationPolicy = new AutomaticRegistrationPolicy();
        foreach (var lib in opts.Libraries)
        {
            var ilib = _sp.GetKeyedService<ILuaCoreLibrary>(lib);
            
            if (ilib == null)
                throw new InvalidOperationException($"Library {lib} not found.");

            ilib.Setup(res);
        }

        return res;
    }

    public static Type ResolveTypeName(string name, bool required = false)
    {
        // Find matching types across all loaded assemblies, with exception handling for broken assemblies
        var matchingTypes = new List<Type>();
        var errAssemblies = new List<string>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var typesInAssembly = assembly.GetTypes()
                    .Where(t => t.Name == name);

                matchingTypes.AddRange(typesInAssembly);
            }
            catch
            {
                errAssemblies.Add(assembly.FullName);
                // ignored
            }
        }

        if (matchingTypes.Count > 1)
            throw new InvalidOperationException($"Found multiple types with name {name}. Please specify the full type name.");

        var res = matchingTypes.FirstOrDefault();

        if (required && res == null)
            throw new ArgumentException($"Type {name} could not be found (could not check {string.Join(',', errAssemblies)}).");

        return res;
    }

    public LuaCore(IOptionsMonitor<LuaCoreOptions> monitor, ILogger logger, [NotNull] IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsChangeMonitorDisposable = monitor.Snapshot(() => _options, o => _options = o, logger, sp);
    }

    public void Dispose()
    {
        _optionsChangeMonitorDisposable.Dispose();
    }

    public async Task<string> RunAsync(string cmd, LuaInstance instance = null, bool printCommand = true)
    {
        instance ??= GetLuaInstance();
        if (printCommand)
            instance.PrintLine($"> {cmd}");
        return await instance.RunAsync(cmd);
    }
}