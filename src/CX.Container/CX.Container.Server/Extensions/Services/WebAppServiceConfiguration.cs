using CX.Container.Server.Resources;
using CX.Container.Server.Services.Auth0;
using ZiggyCreatures.Caching.Fusion;

namespace CX.Container.Server.Extensions.Services;

using Middleware;
using CX.Container.Server.Services;
using System.Text.Json.Serialization;
using Serilog;
using Hellang.Middleware.ProblemDetails;
using Hellang.Middleware.ProblemDetails.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using YoutubeExplode;

public static class WebAppServiceConfiguration
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(Log.Logger);
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddTransient<IAiService, AiService>();
        builder.Services.AddTransient<IConversationCache, ConversationCache>();
        builder.Services.AddTransient<IJwtDecoder, JwtDecoder>();
        builder.Services.AddTransient<IAuth0ClientFactory, Auth0ClientFactory>();
        builder.Services.AddTransient<IAuth0MappingService, Auth0MappingService>();
        builder.Services.AddTransient<IAuth0Service, Auth0Service>();
        builder.Services.AddTransient<OpenAiService, OpenAiService>();
        builder.Services.AddHttpClient<IYouTubeTranscriptService, YouTubeTranscriptService>();                
        builder.Services.AddTransient<ISourceDocumentService, SourceDocumentService>();
        builder.Services.AddTransient<ITextExtractionService, TextExtractionService>();
        builder.Services.AddTransient<IMetadataService, MetadataService>();
        builder.Services.AddTransient<IWebScrapingService, WebScrapingService>();        
        builder.Services.AddTransient<YoutubeClient>();
        builder.Services.AddTransient<IYouTubeService, YouTubeService>();
        builder.Services.AddTransient<IMicrosoftCognitiveSpeechService, MicrosoftCognitiveSpeechService>();

        builder.Services
            .AddProblemDetails(ProblemDetailsConfigurationExtension.ConfigureProblemDetails)
            .AddProblemDetailsConventions();

        // TODO update CORS for your env
        builder.Services.AddCorsService("CX.Container.ServerCorsPolicy", builder.Environment);
        builder.Services.AddInfrastructure(builder.Environment, builder.Configuration);
        //builder.Services.AddMassTransitServices(builder.Environment, builder.Configuration);

        builder.Services
            .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
        builder.Services.AddApiVersioningExtension();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // registers all services that inherit from your base service interface - IAelaServerScopedService
        builder.Services.AddBoundaryServices(Assembly.GetExecutingAssembly());

        builder.Services.AddMvc();

        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerExtension(builder.Configuration);

        // Add Caching
        var defaultCacheOptions = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(30),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromMinutes(30),
            FailSafeThrottleDuration = TimeSpan.FromMinutes(5)
        };
        builder.Services.AddFusionCache(Consts.Cache.Auth.Name).WithDefaultEntryOptions(defaultCacheOptions);
        builder.Services.AddFusionCache(Consts.Cache.Conversation.Name).WithDefaultEntryOptions(defaultCacheOptions);
    }

    /// <summary>
    /// Registers all services in the assembly of the given interface.
    /// </summary>
    private static void AddBoundaryServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (!assemblies.Any())
            throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");

        foreach (var assembly in assemblies)
        {
            var rules = assembly.GetTypes()
                .Where(x => !x.IsAbstract && x.IsClass && x.GetInterface(nameof(IAelaServerScopedService)) == typeof(IAelaServerScopedService));

            foreach (var rule in rules)
            {
                foreach (var @interface in rule.GetInterfaces())
                {
                    services.Add(new ServiceDescriptor(@interface, rule, ServiceLifetime.Scoped));
                }
            }
        }
    }
}
