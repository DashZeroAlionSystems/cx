namespace CX.Container.Server.Configurations
{
    public class AwsSystemOptions
    {
        public string PublicBucket { get; set; } = String.Empty;
        public string PrivateBucket { get; set; } = String.Empty;
        
        public string Region { get; set; }
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string Session { get; set; }

    }
}
