using JetBrains.Annotations;

namespace CX.Engine.Common.Tracing.Langfuse;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class LangfuseOptions
{
    public bool Enabled { get; set; }
    public bool TraceImports { get; set; }
    public string BaseUrl { get; set; } = null!;
    public string PublicKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    
    public Uri BaseUri = null!;
    
    public void Validate()
    {
        if (!Enabled)
            return;
        
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException($"{nameof(LangfuseOptions)}.{nameof(BaseUrl)} is required");
        
        if (string.IsNullOrWhiteSpace(PublicKey))
            throw new ArgumentException($"{nameof(LangfuseOptions)}.{nameof(PublicKey)} is required");
        
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new ArgumentException($"{nameof(LangfuseOptions)}.{nameof(SecretKey)} is required");
        
        //BaseUrl has to be a valid URL
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out BaseUri!))
            throw new ArgumentException($"{nameof(LangfuseOptions)}.{nameof(BaseUrl)} is not a valid URL");
        
        if (!PublicKey.StartsWith("pk-"))
            throw new ArgumentException($"{nameof(LangfuseOptions)}.{nameof(PublicKey)} must start with 'pk-'");
        
        if (!SecretKey.StartsWith("sk-"))
            throw new ArgumentException($"{nameof(LangfuseOptions)}.{nameof(SecretKey)} must start with 'sk-'");
    }
    
}