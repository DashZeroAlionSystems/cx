using System.Diagnostics;
using System.Reflection;
using CX.Container.Server.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CX.Container.Server.Options;

/// <summary>
/// Configures the Swagger generation options.
/// </summary>
/// <remarks>This allows API versioning to define a Swagger document per API version after the
/// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
internal class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    readonly IApiVersionDescriptionProvider _provider;
    readonly ILogger<ConfigureSwaggerOptions> _logger;
    readonly AuthOptions _authOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
    /// </summary>
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, ILogger<ConfigureSwaggerOptions> logger, IOptions<AuthOptions> authOptions)
    {
        _provider = provider;
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    /// <inheritdoc />
    public void Configure(SwaggerGenOptions options)
    {
        // add a swagger document for each discovered API version
        // note: you might choose to skip or document deprecated API versions differently
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
        
        options.CustomSchemaIds(type => type.ToString().Replace("+", "."));
        options.MapType<DateOnly>(() => new OpenApiSchema
        {
            Type = "string",
            Format = "date"
        });

        var xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml");
        if (File.Exists(xmlFilePath))
            options.IncludeXmlComments(xmlFilePath);
        else
            _logger.LogInformation($"Xml comments disabled: File not found {Assembly.GetEntryAssembly()?.GetName().Name}.xml");
        
        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{_authOptions.Authority}/connect/authorize"),
                    TokenUrl = new Uri($"{_authOptions.Authority}/connect/token"),
                    Scopes = new Dictionary<string, string> {
                        { "openid", "User information" },
                        { "role", "Role access for api execution" },
                        { "api", "Used for api execution" },
                        { "offline_access", "Offline access for api execution" },
                        { "profile", "Profile access for api execution" }
                    }
                },
            }
        });
        
        options.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "oauth2"
                    },
                    Scheme = "oauth2",
                    Name = "oauth2",
                    In = ParameterLocation.Header
                },
                new List<string>() { "openid", "role", "profile", "offline_access", "api" }
            }
        });
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        var info = new OpenApiInfo
        {
            Title = $"{versionInfo.ProductName} APIs",
            Version = versionInfo.FileVersion,
        };

        if (description.IsDeprecated)
            info.Description += " This API version has been deprecated.";

        return info;
    }
}