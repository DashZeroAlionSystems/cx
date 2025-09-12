using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace CX.Container.Server.Extensions.Application;
using Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Resources;
using Swashbuckle.AspNetCore.SwaggerUI;

public static class SwaggerAppExtension
{
    public static void UseSwaggerExtension(this IApplicationBuilder app, IConfiguration configuration, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();

        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swagger, httpReq) =>
            {
                if (!httpReq.Headers.ContainsKey("x-envoy-original-path")) return;

                // Set the httpScheme to https if not localhost
                var scheme = "https";
                if (!httpReq.IsHttps && (httpReq.Host.Value.StartsWith("localhost") || httpReq.Host.Value.StartsWith("127.0.0.1") || httpReq.Host.Value.StartsWith("::1")))
                    scheme = "http";

                // Get the original path from the gateway and remove the swagger definition from the path
                var originalPath = httpReq.Headers["x-envoy-original-path"].ToString();
                originalPath = Regex.Replace(originalPath, @"\/swagger\/\d+(\.\d+)?\/swagger\.json", string.Empty);
                var versionProvider = app.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
                if (versionProvider != null && versionProvider.ApiVersionDescriptions.Count > 1)
                    originalPath = versionProvider.ApiVersionDescriptions.Aggregate(originalPath, (current, description) => current.Replace($"/swagger/{description.GroupName}/swagger.json", string.Empty));

                // Create a new server list that contains the 
                swagger.Servers = new List<OpenApiServer>()
                {
                    new() { Url = $"{scheme}://{httpReq.Host.Value}{originalPath}" }
                };
            });
        });
        
        app.UseSwaggerUI(options =>
        {
            options.ConfigObject.DeepLinking = true;
            options.ConfigObject.DisplayRequestDuration  = true;
            options.ConfigObject.Filter = string.Empty;
                
            options.EnableTryItOutByDefault();
            options.DocExpansion(DocExpansion.None);
            
            var authOptions = configuration.GetSection(nameof(AuthOptions)).Get<AuthOptions>();
            options.OAuthClientId("swagger.api");
            options.OAuthUsePkce();
            options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>() { { "audience", authOptions.Audience } });
  
            var versionProvider = app.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
            if (versionProvider == null)
                return;
                
            foreach (var description in versionProvider.ApiVersionDescriptions)
                options.SwaggerEndpoint($"./{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        });
    }
}