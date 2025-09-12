using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
namespace CX.Container.Server.Extensions.Services;

using Databases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Resources;
using CX.Container.Server.Services;
using Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Npgsql;

public static class ServiceRegistration
{
    public static void AddInfrastructure(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // DbContext -- Do Not Delete
        var connectionString = configuration.GetConnectionString("default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // this makes local migrations easier to manage. feel free to refactor if desired.
            connectionString = env.IsDevelopment()
                ? "Host=localhost;Port=5432;Database=aela-db;Username=admin;Password=postgres;"
                : throw new Exception("The database connection string is not set.");
        }

        services.AddDbContext<AelaDbContext>(options =>
            options.UseNpgsql(connectionString,
                    builder =>
                    {
                        builder.MigrationsAssembly(typeof(AelaDbContext).Assembly.FullName);
                    })
                .UseSnakeCaseNamingConvention()); // Fix: EnableDynamicJson should be called after UseSnakeCaseNamingConvention

        services.AddDbContext<AelaDbReadContext>(options =>
            options.UseNpgsql(connectionString,
                    builder => builder.MigrationsAssembly(typeof(AelaDbReadContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddHostedService<MigrationHostedService<AelaDbContext>>();
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        // Auth -- Do Not Delete
        var authOptions = configuration.GetSection(nameof(AuthOptions)).Get<AuthOptions>();
        if (!env.IsEnvironment(Consts.Testing.FunctionalTestingEnvName))
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = authOptions.Authority;
                    options.RequireHttpsMetadata = !env.IsDevelopment();
                    options.IncludeErrorDetails = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences = authOptions.Audience.Split(" ", StringSplitOptions.RemoveEmptyEntries),
                        NameClaimType = JwtClaimTypes.Subject,
                    };
                    options.MapInboundClaims = true;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        // Authz config provided by Auth0. Disabled as we're using Heimgard below.
        // services.AddAuthorization(options =>
        // {
        //     options.AddPolicy(
        //         "read:messages",
        //         policy => policy.Requirements.Add(
        //             new HasScopeRequirement("read:messages", "domain")
        //         )
        //     );
        // });
        //
        // services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
    }
}
