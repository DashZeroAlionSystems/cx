using System.Text.Json;
using Azure.Core;
using CX.Engine.Assistants.Channels;
using CX.Engine.Common;
using CX.Engine.DemoConsole;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class MoonyApiService : IHostedService, IDisposable
{
    private readonly ILogger<MoonyApiService> _logger;
    private readonly MoonyConsoleService _moony;
    private readonly IServiceProvider _sp;
    private IWebHost _webHost;
    private readonly IDisposable _optionsMonitorDisposable;
    private Snapshot _snapshot;

    private class Snapshot
    {
        public MoonyApiServiceOptions Options;
    }

    private void SetSnapshot(MoonyApiServiceOptions options)
    {
        var ss = new Snapshot();
        ss.Options = options;
        _snapshot = ss;
    }

    public MoonyApiService([NotNull] ILogger<MoonyApiService> logger, [NotNull] MoonyConsoleService moonyConsoleService, IOptionsMonitor<MoonyApiServiceOptions> options,
        [NotNull] IServiceProvider sp)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _moony = moonyConsoleService ?? throw new ArgumentNullException(nameof(moonyConsoleService));
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _optionsMonitorDisposable = options.Snapshot(() => _snapshot?.Options, SetSnapshot, logger, sp);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Moony API Service...");
        var ss = _snapshot;

        _webHost = WebHost.CreateDefaultBuilder()
            .UseKestrel()
            .UseUrls(_snapshot.Options.Url) // adjust URL/port as needed
            .ConfigureServices(services => { 
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll",
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                        });
                });     
            })
                .Configure(app =>
            {
                app.UseCors("AllowAll");
                // Use a single middleware to handle all requests.
                app.Run(async context =>
                {
                    // If it's a preflight request:
                    if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                    {
                        // The CORS middleware should set the appropriate headers automatically,
                        // but you can manually set them if needed:
                        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                        context.Response.Headers["Access-Control-Allow-Headers"] = "*";
                        context.Response.Headers["Access-Control-Allow-Methods"] = "*";
        
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                        return;
                    }

                    if (context.Request.Path.Equals("/documents", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                            await context.Response.WriteAsync("Only GET methods are supported.");
                            return;
                        }

                        var query = context.Request.Query;
                        if (!query.TryGetValue("id", out var id))
                        {
                            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                            await context.Response.WriteAsync("No 'id' query parameter.");
                            return;
                        }

                        if (!query.TryGetValue("name", out var name))
                        {
                            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                            await context.Response.WriteAsync("No 'name' query parameter.");
                            return;
                        }

                        if (!query.TryGetValue("store_provider", out var storeProvider))
                        {
                            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                            await context.Response.WriteAsync("No 'store_provider' query parameter.");
                            return;
                        }
                        
                        //Remove here add to store
                        var store = _sp.GetRequiredNamedService<IStorageService>(storeProvider);
                        var content = await store.GetContentAsync(id);
                        if (content == null)
                        {
                            context.Response.StatusCode = 404;
                            return;
                        }
                        
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        context.Response.ContentType = content.ContentType.GetContentType();
                        context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{name}\"";
                        // copy into the HTTP response stream
                        await content.Content.CopyToAsync(context.Response.Body, cancellationToken);
                        return;
                    }
                    // Check if the request is for /moony/send.
                    if (context.Request.Path.Equals("/moony/send", StringComparison.OrdinalIgnoreCase))
                    {
                        // Only allow POST requests.
                        if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                            await context.Response.WriteAsync("Only POST method is allowed.");
                            return;
                        }

                           // Ensure the request has form content.
                        if (!context.Request.HasFormContentType)
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Expected form data content type.");
                            return;
                        }

                        // Read the form data.
                        var form = await context.Request.ReadFormAsync();

                        if (!form.ContainsKey("cmd"))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Missing 'cmd' field in form data.");
                            return;
                        }


                        string channelName = form["channel-name"];
                        string cmd = form["cmd"];
                        string res;
                        if (!string.IsNullOrWhiteSpace(channelName))
                        {
                            var channel = _sp.GetRequiredNamedService<Channel>(channelName);
                            res = (await channel.Assistant.AskAsync(cmd, new() { UserId = "MoonyApi", SessionId = Guid.NewGuid().ToString() }))?.Answer;
                        }
                        else
                            res = await ProcessInputAsync(cmd); 
                        
                        //Return the result as a JSON "result" property in an object.
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { Result = res }, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else if (context.Request.Path.Equals("/moony/channels", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(ss.Options.Channels ?? []));
                    }
                    else
                    {
                        // For all other endpoints, return a 404.
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync("Not Found");
                    }
                });
            })
            .Build();

        // Start the web host in the background.
        await _webHost.StartAsync(cancellationToken);

        _logger.LogInformation("Started Moony API Service.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Moony API Service...");
        if (_webHost != null)
            await _webHost.StopAsync(cancellationToken);
        _logger.LogInformation("Stopped Moony API Service.");
    }

    /// <summary>
    /// Processes the input string. Modify this method to include your logic.
    /// </summary>
    private async Task<string> ProcessInputAsync(string input)
    {
        return await _moony.RunAsync(input);
    }

    public void Dispose()
    {
        _webHost?.Dispose();
        _optionsMonitorDisposable?.Dispose();
    }
}
