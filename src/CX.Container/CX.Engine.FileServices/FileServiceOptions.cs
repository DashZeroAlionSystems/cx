using JetBrains.Annotations;

namespace CX.Engine.FileServices;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class FileServiceOptions
{
    public string FileCacheDirectory { get; set; } = null!;
    
    public void Validate()
    {
        if (!Directory.Exists(FileCacheDirectory))
            throw new InvalidOperationException("File cache directory does not exist: " + FileCacheDirectory);
    }
}