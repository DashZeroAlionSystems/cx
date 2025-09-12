using System.Text.Json;
using CX.Engine.Common.ACL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CX.Engine.Common.Tests;

public class ACLServiceTests
{
    private ACLService GetConfigured(ACLServiceOptions opts) => GetConfigured(JsonSerializer.Serialize(new { ACLService = opts }));

    private ACLService GetConfigured(string config)
    {
        var sc = new ServiceCollection();
        sc.AddLogging();
        var cb = new ConfigurationBuilder();
        cb.AddJsonString(config);
        var configuration = cb.Build();

        sc.AddACLService(configuration);
        var sp = sc.BuildServiceProvider();
        return sp.GetRequiredService<ACLService>();
    }

    [Fact]
    public void Basics()
    {
        const string APIKeyCanOnlyFuberGet = "APIKey-CanOnlyFubarGet";
        const string APIKeySuper = "APIKey-Super";
        const string APIKeyDoesNotExist = "APIKey-DoesNotExist";
        const string APIKeyFubarAll = "APIKey-FubarAll";
        const string APIKeyDenyAndAllowAll = "APIKey-DenyAll";
        const string FubarGet = "fubar://get";
        const string FubarPost = "fubar://post";
        const string RebarGet = "rebar://get";

        var svc = GetConfigured(new ACLServiceOptions()
        {
            APIKeys =
            {
                [APIKeyCanOnlyFuberGet] = new()
                {
                    Allow = [FubarGet]
                },
                [APIKeySuper] = new()
                {
                    Allow = ["^.*$"]
                },
                [APIKeyFubarAll] = new()
                {
                    Allow = ["^fubar://.*$"]
                },
                [APIKeyDenyAndAllowAll] = new()
                {
                    Allow = ["^.*$"],
                    Deny = ["^.*$"]
                }
            }
        });

        Assert.True(svc.IsAllowed(APIKeyCanOnlyFuberGet, FubarGet));
        Assert.False(svc.IsAllowed(APIKeyCanOnlyFuberGet, FubarPost));

        Assert.True(svc.IsAllowed(APIKeySuper, FubarGet));
        Assert.True(svc.IsAllowed(APIKeySuper, FubarPost));
        
        Assert.False(svc.IsAllowed(APIKeyDoesNotExist, FubarGet));
        Assert.False(svc.IsAllowed(APIKeyDoesNotExist, FubarPost));
        
        Assert.True(svc.IsAllowed(APIKeyFubarAll, FubarGet));
        Assert.True(svc.IsAllowed(APIKeyFubarAll, FubarPost));
        Assert.False(svc.IsAllowed(APIKeyFubarAll, RebarGet));
        
        Assert.False(svc.IsAllowed(APIKeyDenyAndAllowAll, FubarGet));
        Assert.False(svc.IsAllowed(APIKeyDenyAndAllowAll, FubarPost));
        Assert.False(svc.IsAllowed(APIKeyDenyAndAllowAll, RebarGet));
    }
}