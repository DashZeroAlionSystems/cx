using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace CX.Container.Server.Extensions.Services;
using Microsoft.AspNetCore.Mvc;

public static class ApiVersioningServiceExtension
{
    public static void AddApiVersioningExtension(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
        
        services.AddVersionedApiExplorer(options =>
        {
            options.SubstituteApiVersionInUrl = true;  
            options.GroupNameFormat = "'v'VVV";
        });
        
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.UseApiBehavior = true;
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }
}