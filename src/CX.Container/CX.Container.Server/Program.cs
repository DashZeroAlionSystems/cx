using Hellang.Middleware.ProblemDetails;
using CX.Container.Server.Extensions.Application;
using CX.Container.Server.Extensions.Services;
using CX.Container.Server.Hubs.Chat;
using CX.Container.Server.Middleware;
using CX.Clients.Afriforum.Domain;
using CX.Clients.Weelee.Domain;
using CX.Engine.Archives.Pinecone;
using CX.Engine.Common;
using CX.Engine.Common.RegistrationServices;
using CX.Engine.DocExtractors.Text;
using CX.Engine.Importing;
using CX.Engine.Registration.Full;
using Microsoft.IdentityModel.Logging;
using Npgsql;
using OpenTelemetry;
using Sentry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.IO;

try
{
    FullProfile.Register();
    SakenetwerkAssistant.Register();
    WeeleeTestKit.Register();

    var builder = WebApplication.CreateBuilder(args);
    
    // Configure Sentry
    SentrySdk.Init(o =>
    {
        o.Dsn = "https://3b6b25709b7140d22fb8fff9bc652731@o4508839455096832.ingest.de.sentry.io/4508839456669776";
        o.Debug = false;
        o.TracesSampleRate = 1.0;
    });
    
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = "https://3b6b25709b7140d22fb8fff9bc652731@o4508839455096832.ingest.de.sentry.io/4508839456669776";
        o.Debug = false;
        o.TracesSampleRate = 1.0;
        o.InitializeSdk = true;
    });
   
    builder.Configuration.AddJsonFile("config/appsettings.json", true, true);
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly);
    builder.ConfigureEnvironmentVariables();
    
    // Configure OpenTelemetry - simplified version
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName: "CXContainer", serviceVersion: "1.0"))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://localhost:4317");
                opts.ExportProcessorType = ExportProcessorType.Batch;
            }))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddSource("CXContainer")
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://localhost:4317");
                opts.ExportProcessorType = ExportProcessorType.Batch;
            }));
    
    builder.Services.RegisterAwsExtension(builder.Configuration, builder.Environment);
    builder.Services.RegisterAiExtension(builder.Configuration, builder.Environment);
    
    builder.Services.AddCXEngine(builder.Configuration);
    builder.ConfigureServices();
    var app = builder.Build();
    
    app.UseSentryTracing();
    SentrySdk.CaptureMessage("Hello Sentry");
    
    await app.StartCXEngineAsync();

    using var scope = app.Services.CreateScope();
    if (builder.Environment.IsDevelopment())
    {
        IdentityModelEventSource.ShowPII = true;
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        app.UseDeveloperExceptionPage();
    }
    else
    {
        IdentityModelEventSource.ShowPII = true;
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    
    app.UseProblemDetails();
    app.UseHttpsRedirection();
    app.UseCors("CX.Container.ServerCorsPolicy");
    // Serve static files (e.g., landing page or future wwwroot assets)
    app.UseStaticFiles();

    // Mount the Workflow Builder static app at /workflow if present
    var workflowDir = Path.Combine(app.Environment.ContentRootPath, "React", "WorkflowBuilder");
    if (Directory.Exists(workflowDir))
    {
        var workflowProvider = new PhysicalFileProvider(workflowDir);
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = workflowProvider,
            RequestPath = "/workflow",
            DefaultFileNames = new List<string> { "index.html" }
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = workflowProvider,
            RequestPath = "/workflow"
        });
    }
    app.MapHealthChecks("api/health");
    app.UseRouting();
    app.UseApiVersioning();
    app.UseAuthentication();
    app.UseMiddleware<JwtBearerLoggingMiddleware>();
    app.UseAuthorization();
    app.MapControllers();
    app.UseSwaggerExtension(builder.Configuration, builder.Environment);

    // Simple redirect landing page at /
    app.MapGet("/", () => Results.Redirect("/swagger", permanent: false));
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

// Make the implicit Program class public so the functional test project can access it
public partial class Program { }