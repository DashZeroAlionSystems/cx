using CX.Engine.Common.LuaScripting;
using CX.Engine.Common.Stores.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common;

public static class LuaCoreDI
{
    public const string ConfigurationSection = "LuaCores";
    public const string ConfigurationTableName = "config_luacore";
    public const string LuaCoreLibraryServiceProvider = "ServiceProvider";
    public const string LuaStdLibrary = "LuaStd";

    public static void AddLuaCore(this IServiceCollection sc, IConfiguration configuration)
    {
        sc.AddTypedJsonConfigTable<LuaCoreOptions>(configuration, ConfigurationSection, ConfigurationTableName);
        sc.AddKeyedSingleton<ILuaCoreLibrary>(LuaCoreLibraryServiceProvider, (sp, _) => new ServiceProviderLua(sp));
        sc.AddKeyedSingleton<ILuaCoreLibrary>(LuaStdLibrary, (sp, _) => new LuaStd());
        sc.AddNamedSingletons<LuaCore>(configuration, (sp, config, name, optional) =>
        {
            if (optional && !config.SectionExists(ConfigurationSection, name))
                return null;
            
            var opts = config.MonitorRequiredSection<LuaCoreOptions>(ConfigurationSection, name);
            var logger = sp.GetLogger<LuaCore>(name);
            return new (opts, logger, sp);
        });
    }
}