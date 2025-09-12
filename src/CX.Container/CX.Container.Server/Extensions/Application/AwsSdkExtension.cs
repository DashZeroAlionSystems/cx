using CX.Container.Server.Configurations;
using CX.Container.Server.Resources;
using Amazon.S3;
using Amazon.Runtime;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using CX.Container.Server.Wrappers;

public static class AwsSdkExtension
{
    public static void RegisterAwsExtension(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        if (!env.IsEnvironment(Consts.Testing.FunctionalTestingEnvName))
        {
            var config = configuration.GetSection(nameof(AwsSystemOptions)).Get<AwsSystemOptions>();
            var awsRegion = config.Region ?? "";
            var options = new AWSOptions
            {
                Credentials = new BasicAWSCredentials(config.AccessKeyId ?? "", config.SecretAccessKey ?? ""),
                SessionName = config.Session ?? ""
            };
            if(!string.IsNullOrEmpty(awsRegion))
            {
                options.Region = RegionEndpoint.GetBySystemName(awsRegion);
            }
            services.AddDefaultAWSOptions(options);

            services.AddAWSService<IAmazonS3>();
            services.AddSingleton<IFileProcessing, AwsFileProcessing>();
        }
    }
}