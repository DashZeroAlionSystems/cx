namespace CX.Engine.Common.Testing;

public sealed class TestHostBuilder
{
    public readonly ITestOutputHelper TestOutputHelper;
    public ITestContext DefaultContext;
    public IConfiguration Configuration;

    public TestHostBuilder(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public event Action<IConfigurationBuilder> SetupConfiguration;
    public event Action<IServiceCollection, IConfiguration> SetupServices;

    public IServiceProvider BuildServiceProviderAndConfiguration()
    {
        var cb = new ConfigurationBuilder();

        SetupConfiguration?.Invoke(cb);
        
        cb.AddJsonFile("appsettings.json", true)
          .AddUserSecrets(typeof(TestHostBuilder).Assembly, true);

        Configuration = cb.Build();

        var sc = new ServiceCollection();
        sc.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new XunitLoggerProvider(TestOutputHelper));
        });

        SetupServices?.Invoke(sc, Configuration);

        return sc.BuildServiceProvider();
    }

    public async Task RunAsync(ITestContext context, Func<Task> task)
    {
        var sp = BuildServiceProviderAndConfiguration();
        var hostedServices = sp.GetServices<IHostedService>().ToArray();

        foreach (var service in hostedServices)
            await service.StartAsync(CancellationToken.None);
        
        (context ?? DefaultContext)?.Ready(sp);

        try
        {
            await task();
        }
        finally
        {
            foreach (var service in hostedServices)
                await service.StopAsync(CancellationToken.None);
        }
    }

    public Task RunAsync(Func<Task> task) => RunAsync(null, task);

    public Task RunAsync(Action a) => RunAsync(null, () =>
    {
        a();
        return Task.CompletedTask;
    });
}