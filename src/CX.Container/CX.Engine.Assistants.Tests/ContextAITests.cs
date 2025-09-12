using CX.Engine.Assistants.ContextAI;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CXLibTests;

public class ContextAITests : TestBase
{
    private ContextAIService _contextAI = null!;
    private ILogger<ContextAITests> _logger = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _contextAI = sp.GetRequiredService<ContextAIService>();
        _logger = sp.GetRequiredService<ILogger<ContextAITests>>();
    }

    [Fact]
    public Task CanLogThreadMessagesTest() => Builder.RunAsync(async () => 
    {
        var initialId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.AddMinutes(-1);
        var conversationid = await _contextAI.Enqueue(new LogThreadMessageRequest(initialId, "user", "bob", "hi there", now));
        _logger.LogInformation("Conversation Id: " + conversationid); 
    });
    
    [Fact]
    public Task CanLogThreadToolUseTest() => Builder.RunAsync(async () => 
    {
        var initialId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.AddMinutes(-1);
        var conversationid = await _contextAI.Enqueue(new LogThreadToolUseRequest(initialId, "attach", "A.txt", "bob", now));
        _logger.LogInformation("Conversation Id: " + conversationid); 
    });

    [Fact]
    public Task CanLogConvoTest() => Builder.RunAsync(async () =>
    {
        var initialId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.AddMinutes(-1);
        var msg1id = await _contextAI.Enqueue(new LogThreadMessageRequest(initialId, "user", "bob", "hi there", now));
        var msg2id = await _contextAI.Enqueue(new LogThreadMessageRequest(initialId, "system", "bob", "Hi Bob, how can I help you today?", now.AddSeconds(30)));
        var msg3id = await _contextAI.Enqueue(new LogThreadToolUseRequest(initialId, "attach", "mock.txt", "bob", now.AddSeconds(30)));
        var msg4id = await _contextAI.Enqueue(new LogThreadMessageRequest(initialId, "user", "bob", "how are you?", now.AddSeconds(45)));
        _logger.LogInformation("Conversation Ids: " + msg1id + ", " + msg2id + ", " + msg3id + ", " + msg4id); 
        Assert.Equal(msg1id, msg2id);
        Assert.Equal(msg2id, msg3id);
        Assert.Equal(msg3id, msg4id);
    });

    public ContextAITests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(SecretNames.ContextAI.Enabled);
        Builder.AddServices((sc, config) =>
        {
            sc.AddContextAI(config);
        });
    }
}