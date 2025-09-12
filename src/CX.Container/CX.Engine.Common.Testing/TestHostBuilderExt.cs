namespace CX.Engine.Common.Testing;

public static class TestHostBuilderExt
{
    public static TestHostBuilder OnSetupConfiguration(this TestHostBuilder host, Action<IConfigurationBuilder> setupConfiguration)
    {
        host.SetupConfiguration += setupConfiguration;
        return host;
    }

    public static TestHostBuilder AddServices(this TestHostBuilder host, Action<IServiceCollection, IConfiguration> setupServices)
    {
        host.SetupServices += setupServices;
        return host;
    }

    public static TestHostBuilder AddConfig(this TestHostBuilder builder, params string[] config)
    {
        return builder.OnSetupConfiguration(cb => cb.AddJsonStrings(config));
    }
    
    public static TestHostBuilder AddJsonFileConfig(this TestHostBuilder builder, string file)
    {
        return builder.OnSetupConfiguration(cb => cb.AddJsonFile(file, false, true));
    }
    
}