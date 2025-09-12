namespace CX.Engine.Common.Testing;

public class TestBase : ITestContext
{
    public readonly TestHostBuilder Builder;
    
    public TestBase(ITestOutputHelper testOutputHelper)
    {
        Builder = new(testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper)));
        Builder.DefaultContext = this;
    }
    
    void ITestContext.Ready(IServiceProvider sp) => ContextReady(sp);

    protected virtual void ContextReady(IServiceProvider sp)
    {
    }
}