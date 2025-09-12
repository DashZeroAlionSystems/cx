using CX.Engine.ChatAgents;
using CX.Engine.ChatAgents.OpenAI;
using CX.Engine.Common;
using CX.Engine.Common.Testing;
using CX.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace CXLibTests;

public class ChatCacheTests : TestBase
{
    private ChatCache _chatCache = null!;
    private OpenAIChatAgent _gpt4o = null!;

    protected override void ContextReady(IServiceProvider sp)
    {
        _chatCache = sp.GetRequiredService<ChatCache>();
        _gpt4o = (OpenAIChatAgent)sp.GetRequiredNamedService<IChatAgent>("OpenAI.GPT-4o");
    }

    [Fact]
    public Task PickOneTestAsync() => Builder.RunAsync(this, async () =>
    {
        {
            var cacheFile = _chatCache.Options.CacheFile;

            _chatCache.Clear();
            try
            {
                var req = _gpt4o.GetRequest("Reply with a single word.  Which is typically red? Apple, Banana or Lettuce?");
                var res = await _chatCache.ChatAsync(req);
                Assert.Contains("Apple", res.Answer, StringComparison.InvariantCultureIgnoreCase);
                Assert.Equal(0, _chatCache.CacheHits);
                Assert.Equal(1, _chatCache.CacheEntries);

                req = _gpt4o.GetRequest("Reply with a single word.  Which is typically red? Apple, Banana or Lettuce?");
                res = await _chatCache.ChatAsync(req);
                Assert.Contains("Apple", res.Answer, StringComparison.InvariantCultureIgnoreCase);
                Assert.Equal(1, _chatCache.CacheHits);
                Assert.Equal(1, _chatCache.CacheEntries);

                var tmp = Path.GetTempPath();
                var tmpFile = Path.Combine(tmp, "chat.cache");
                _chatCache.Options.CacheFile = tmpFile;
                _chatCache.Save();

                _chatCache.Clear();
                Assert.Equal(0, _chatCache.CacheEntries);

                _chatCache.Load();
                Assert.Equal(1, _chatCache.CacheEntries);
            }
            finally
            {
                _chatCache.Options.CacheFile = cacheFile;
                _chatCache.Clear();
                _chatCache.Load();
            }
        }
    });

    public ChatCacheTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        Builder.AddSecrets(
            SecretNames.ChatCache.LocalDisk,
            SecretNames.OpenAIChatAgents);
        Builder.AddServices((sc, config) =>
        {
            sc.AddChatCache(config);
            sc.AddChatAgents(config);
        });
    }
}