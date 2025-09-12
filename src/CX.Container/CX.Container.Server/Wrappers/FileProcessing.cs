using CX.Container.Server.Configurations;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace CX.Container.Server.Wrappers
{
    public class AwsFileProcessing(IAmazonS3 awsClient, ILogger<AwsFileProcessing> logger,
        IOptions<AwsSystemOptions> awsOptions, TimeProvider clock) : IFileProcessing
    {
        private readonly IAmazonS3 _client = awsClient;
        private readonly ILogger<AwsFileProcessing> _logger = logger;
        private readonly AwsSystemOptions _awsOptions = awsOptions.Value;
        private readonly TimeProvider _clock = clock;

        public async Task<string> CopyFileAsync(string sourceBucket, string destinationBucket, string fileName, CancellationToken cancellationToken)
        {
            var sourceFile = await FileExists(sourceBucket, fileName, cancellationToken);
            if (!sourceFile)
            {
                _logger.LogWarning("File {fileName} does not exist in Source bucket {bucketName}", fileName, sourceBucket);
                throw new Exception($"File {fileName} does not exist in Source bucket {sourceBucket}");
            }
            var destinationFile = await FileExists(destinationBucket, fileName, cancellationToken);
            if (destinationFile)
            {
                //Bug fix: Nodes that had their document deleted in the UI, reuploaded and retrained would fail since they had already been trained once.
                //Overwriting instead of erroring fixes this behaviour.
                await DeleteFileAsync(destinationBucket, fileName, cancellationToken);
                // _logger.LogWarning("File {fileName} already exist in Destination bucket {bucketName}", fileName, destinationBucket);
                // throw new Exception($"File {fileName} already exist in Destination bucket {destinationBucket}");
            }

            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = sourceBucket,
                SourceKey = fileName,
                DestinationBucket = destinationBucket,
                DestinationKey = fileName
            };
            try
            {
                var response = await _client.CopyObjectAsync(copyRequest, cancellationToken);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("File {fileName} copied from {sourceBucket} to {destinationBucket}", fileName, sourceBucket, destinationBucket);
                }
                else
                {
                    _logger.LogError("File {fileName} not copied from {sourceBucket} to {destinationBucket}", fileName, sourceBucket, destinationBucket);
                    throw new Exception($"File {fileName} not copied from {sourceBucket} to {destinationBucket}");
                }
                return $"File {fileName} copied from {sourceBucket} to {destinationBucket}";
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError("Error Processing {fileName} error: {Message}", fileName, ex.Message);
                throw new Exception($"Error Processing {fileName} error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Processing {fileName} error: {Message}", fileName, ex.Message);
                throw new Exception($"Error Processing {fileName} error: {ex.Message}");
            }
        }
        public async Task<string> GetPresignedUrlAsync(string bucketName, string fileName, CancellationToken cancellationToken)
        {
            if (bucketName == string.Empty)
            {
                bucketName = _awsOptions.PrivateBucket;
            }
            var exists = await FileExists(bucketName, fileName, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("File {fileName} does not exist in {bucketName}", fileName, bucketName);
                throw new Exception($"File {fileName} does not exist in {bucketName}");
            }
            await BucketExists(bucketName, cancellationToken);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = fileName,
                Expires = _clock.GetUtcNow().UtcDateTime.AddDays(1)
            };
            var url = _client.GetPreSignedURL(request);
            return url;
        }
        public async Task<string> MoveFileAsync(string sourceBucket, string destinationBucket, string fileName, CancellationToken cancellationToken)
        {
            await BucketExists(sourceBucket, cancellationToken);
            await BucketExists(destinationBucket, cancellationToken);
            var copy = await CopyFileAsync(sourceBucket, destinationBucket, fileName, cancellationToken);
            var delete = await DeleteFileAsync(sourceBucket, fileName, cancellationToken);
            return $"{copy} and {delete}";
        }

        public async Task<string> UploadFileAsync(string bucketName, IFormFile file, CancellationToken cancellationToken)
        {
            var md5FileName = await MD5CheckSumFileName(file);
            var exists = await FileExists(bucketName, md5FileName, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("File {fileName} already exist in {bucketName}", file.FileName, bucketName);
                throw new Exception($"File {file.FileName} already exist in {bucketName}");
            }
            try
            {
                await BucketExists(bucketName, cancellationToken);
                using (var newMemoryStream = new MemoryStream())
                {
                    file.CopyTo(newMemoryStream);
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = newMemoryStream,
                        Key = md5FileName,
                        BucketName = bucketName
                        //CannedACL = S3CannedACL.PublicRead
                    };
                    uploadRequest.Metadata.Add("file-title", file.FileName);
                    var fileTransferUtility = new TransferUtility(_client);
                    await fileTransferUtility.UploadAsync(uploadRequest, cancellationToken);
                }
                return $"File {file.FileName} uploaded to {bucketName}";
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw new Exception($"File {file.FileName} NOT uploaded to {bucketName} {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw new Exception($"File {file.FileName} NOT uploaded to {bucketName} {ex.Message}");
            }
        }
        public async Task<string> DeleteFileAsync(string bucketName, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                var exists = await FileExists(bucketName, fileName, cancellationToken);
                if (!exists)
                {
                    _logger.LogWarning("File {fileName} does not exist in {bucketName}", fileName, bucketName);
                    return string.Empty;
                }
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };
                var deleteResponce = await _client.DeleteObjectAsync(deleteRequest, cancellationToken);
                _logger.LogInformation("File being {fileName} deleted from bucketName {bucketName}", fileName, bucketName);
                if (deleteResponce.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("File {fileName} deleted from {bucketName}", fileName, bucketName);
                    return $"File {fileName} deleted from {bucketName}";
                }
                else
                {
                    _logger.LogWarning("File {fileName} not deleted from {bucketName}", fileName, bucketName);
                    throw new Exception($"File {fileName} not deleted from {bucketName} {deleteResponce.HttpStatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"File {fileName} not deleted from {bucketName} {ex.Message}");
                throw new Exception($"File {fileName} not deleted from {bucketName} {ex.Message}");
            }
        }

        public async Task<bool> FileExists(string bucketName, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                var bucket = await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName);
                if (!bucket)
                {
                    _logger.LogError("Bucket {bucketName} does not exist", bucketName);
                    return false;
                }
                var obj = await _client.GetObjectMetadataAsync(bucketName, fileName, cancellationToken);
                if (obj.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("File {fileName} exists in {bucketName}", fileName, bucketName);
                    return true;
                }
                else
                {
                    _logger.LogWarning("File {fileName} does not exist in {bucketName}", fileName, bucketName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("File {fileName} does not exist in {bucketName} {Exception}", fileName, bucketName, ex.Message);
                return false;
            }
        }
        public async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
        {
            try
            {
                var bucket = await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName);
                if (bucket)
                {
                    _logger.LogInformation("Bucket {bucketName} exists", bucketName);
                    return true;
                }
                else
                {
                    throw new Exception($"Bucket {bucketName} does not exist");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Bucket {bucketName} does not exist {ex.Message}");
            }
        }

        public Task<string> MD5CheckSum(IFormFile file)
        {
            try
            {
                Stream fileStream = file.OpenReadStream();
                MemoryStream memoryStream = new();
                fileStream.CopyTo(memoryStream);
                byte[] hash = MD5.HashData(memoryStream.ToArray());
                return Task.FromResult(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
            }
            catch (Exception ex)
            {
                _logger.LogError("MD5CheckSum checksum failed {Message}", ex.Message);
                throw;
            }
        }
        public async Task<string> MD5CheckSumFileName(IFormFile file)
        {
            try
            {
                var md5Name = await MD5CheckSum(file);
                var fileExtension = file.FileName.Split('.').Last();
                return $"{md5Name}.{fileExtension}";
            }
            catch (Exception ex)
            {
                _logger.LogError("MD5CheckSum checksum failed {Message}", ex.Message);
                throw;
            }
        }
    }

    public interface IFileProcessing
    {
        Task<string> CopyFileAsync(string sourceBucket, string destinationBucket, string fileName, CancellationToken cancellationToken);
        Task<string> MoveFileAsync(string sourceBucket, string destinationBucket, string fileName, CancellationToken cancellationToken);
        Task<string> UploadFileAsync(string bucketName, IFormFile file, CancellationToken cancellationToken);
        Task<string> DeleteFileAsync(string bucketName, string fileName, CancellationToken cancellationToken);
        Task<string> GetPresignedUrlAsync(string bucketName, string fileName, CancellationToken cancellationToken);
        Task<bool> FileExists(string bucketName, string fileName, CancellationToken cancellationToken);
        Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken);
        Task<string> MD5CheckSum(IFormFile file);
        Task<string> MD5CheckSumFileName(IFormFile file);
    }
}