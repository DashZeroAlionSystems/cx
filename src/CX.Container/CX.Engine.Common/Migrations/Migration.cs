using JetBrains.Annotations;

namespace CX.Engine.Common.Migrations;

public class Migration
{
    public readonly Func<IServiceProvider, Task> OnRunAsync;
    
    public Migration([NotNull] Func<IServiceProvider, Task> onRunAsync)
    {
        OnRunAsync = onRunAsync ?? throw new ArgumentNullException(nameof(onRunAsync));
    }

    public Task RunAsync(IServiceProvider sp)
    {
        if (OnRunAsync == null)
            throw new InvalidOperationException("Migration has no OnRunAsync method");
        
        return OnRunAsync(sp);
    }
}