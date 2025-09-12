using CX.Container.Server.Configurations;

namespace CX.Container.Server.Extensions.Services;

using CX.Container.Server.Options;
using CX.Engine.SharedOptions;
using Microsoft.Extensions.DependencyInjection;

public static class OptionsConfiguration
{
    public static void ConfigureEnvironmentVariables(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AiOptions>()
            .BindConfiguration(nameof(AiOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddOptions<AwsSystemOptions>()
            .BindConfiguration(nameof(AwsSystemOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddOptions<AuthOptions>()
            .BindConfiguration(nameof(AuthOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<OpenAiOptions>()
            .BindConfiguration(nameof(OpenAiOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<StructuredDataOptions>()
            .BindConfiguration(nameof(StructuredDataOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<WeeleeOptions>()
            .BindConfiguration(nameof(WeeleeOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<CognitiveSpeechOptions>()
            .BindConfiguration(nameof(CognitiveSpeechOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
