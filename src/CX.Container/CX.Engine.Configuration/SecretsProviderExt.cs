using CX.Engine.Common;
using CX.Engine.Common.Testing;
using Microsoft.Extensions.Configuration;

namespace CX.Engine.Configuration;

public static class SecretsProviderExt
{
    public static void AddSecrets<T>(this T configuration, params string[] secretNames) where T: IConfigurationBuilder
    {
        foreach (var secretName in secretNames)
            configuration.AddJsonFile(SecretsProvider.GetPath(secretName), false, true);
    }

    public static TestHostBuilder AddSecrets(this TestHostBuilder builder, params string[] secretNames)
    {
        foreach (var secretName in secretNames)
            builder.AddJsonFileConfig(SecretsProvider.GetPath(secretName));

        return builder;
    }
}