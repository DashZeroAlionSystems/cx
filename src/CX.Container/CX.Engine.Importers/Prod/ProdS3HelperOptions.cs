using JetBrains.Annotations;

namespace CX.Engine.Importing.Prod;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ProdS3HelperOptions
{
    public string PublicBucket { get; set; } = null!;
    public string PrivateBucket { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string Session { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PublicBucket))
            throw new InvalidOperationException($"{nameof(ProdS3HelperOptions)}.{nameof(PublicBucket)} is required");

        if (string.IsNullOrWhiteSpace(PrivateBucket))
            throw new InvalidOperationException($"{nameof(ProdS3HelperOptions)}.{nameof(PrivateBucket)} is required");

        if (string.IsNullOrWhiteSpace(Region))
            throw new InvalidOperationException($"{nameof(ProdS3HelperOptions)}.{nameof(Region)} is required");

        if (string.IsNullOrWhiteSpace(AccessKeyId))
            throw new InvalidOperationException($"{nameof(ProdS3HelperOptions)}.{nameof(AccessKeyId)} is required");

        if (string.IsNullOrWhiteSpace(SecretAccessKey))
            throw new InvalidOperationException($"{nameof(ProdS3HelperOptions)}.{nameof(SecretAccessKey)} is required");

        if (string.IsNullOrWhiteSpace(Session))
            throw new InvalidOperationException($"{nameof(ProdS3HelperOptions)}.{nameof(Session)} is required");
    }
}