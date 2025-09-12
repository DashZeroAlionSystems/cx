using Amazon;
using Amazon.Runtime;
using Amazon.S3;

namespace CX.Engine.Importing.Prod;

public class ProdS3Helper
{
    public readonly ProdS3HelperOptions Options;
    private readonly AmazonS3Client _client;

    public ProdS3Helper(ProdS3HelperOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();

        _client = new(new BasicAWSCredentials(Options.AccessKeyId, Options.SecretAccessKey),
            new AmazonS3Config()
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            });
    }

    public async Task<Stream> GetObjectAsync(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        var priv = await GetObjectAsync(Options.PrivateBucket, key);
        var pub = await GetObjectAsync(Options.PublicBucket, key);
        return priv ?? pub;
    }

    public async Task<Stream> GetObjectAsync(string bucket, string key)
    {
        try
        {
            var response = await _client.GetObjectAsync(new()
            {
                BucketName = bucket,
                Key = key,
            });

            return response.ResponseStream!;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            else
                throw;
        }
    }
}