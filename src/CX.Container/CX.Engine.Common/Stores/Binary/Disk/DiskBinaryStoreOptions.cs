using JetBrains.Annotations;

namespace CX.Engine.Common.Stores.Binary.Disk;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DiskBinaryStoreOptions
{
    public string Folder { get; set; } = null!;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Folder))
            throw new InvalidOperationException($"Missing {nameof(DiskBinaryStoreOptions)}.{nameof(Folder)}");
    }

}