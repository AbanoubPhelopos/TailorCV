using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Infrastructure.Storage;

public class S3BlobStorage : IBlobStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<S3BlobStorage> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public S3BlobStorage(
        IAmazonS3 s3Client,
        IOptions<BlobStorageOptions> options,
        ILogger<S3BlobStorage> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _options.BucketName);

        if (!exists)
        {
            _logger.LogInformation("Creating S3 bucket {Bucket}", _options.BucketName);
            await _s3Client.PutBucketAsync(_options.BucketName, ct);
            _logger.LogInformation("Created S3 bucket {Bucket}", _options.BucketName);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        PutObjectRequest putRequest = new()
        {
            BucketName = _options.BucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = contentType,
        };

        await _s3Client.PutObjectAsync(putRequest, ct);

        _logger.LogInformation("Uploaded {FileName} to bucket {Bucket}", fileName, _options.BucketName);

        return fileName;
    }

    public async Task<Stream?> DownloadAsync(string fileKey, CancellationToken ct = default)
    {
        try
        {
            GetObjectRequest request = new()
            {
                BucketName = _options.BucketName,
                Key = fileKey,
            };

            GetObjectResponse response = await _s3Client.GetObjectAsync(request, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "File {FileKey} not found in bucket {Bucket}", fileKey, _options.BucketName);
            return null;
        }
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        DeleteObjectRequest request = new()
        {
            BucketName = _options.BucketName,
            Key = fileKey,
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public string GetPublicLink(string fileKey)
    {
        string endpoint = _options.Endpoint.TrimEnd('/');
        return $"{endpoint}/{_options.BucketName}/{fileKey}";
    }

    public async Task MoveAsync(string sourceKey, string destinationKey, CancellationToken ct = default)
    {
        CopyObjectRequest copyRequest = new()
        {
            SourceBucket = _options.BucketName,
            SourceKey = sourceKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destinationKey,
        };

        await _s3Client.CopyObjectAsync(copyRequest, ct);
        await DeleteAsync(sourceKey, ct);

        _logger.LogInformation("Moved {Source} to {Destination} in bucket {Bucket}", sourceKey, destinationKey, _options.BucketName);
    }

    public async Task<bool> ExistsAsync(string fileKey, CancellationToken ct = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_options.BucketName, fileKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<PresignedPostResponse> GeneratePresignedPostAsync(
        string fileKey,
        string contentType,
        long maxSizeBytes,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        CreatePresignedPostRequest request = new()
        {
            BucketName = _options.BucketName,
            Key = fileKey,
            Expires = (_dateTimeProvider.UtcNow + expiry).UtcDateTime,
            Conditions =
            [
                new ContentLengthRangeCondition(0, maxSizeBytes),
                new StartsWithCondition("Content-Type", contentType),
            ],
        };

        CreatePresignedPostResponse response = await _s3Client.CreatePresignedPostAsync(request);

        return new PresignedPostResponse(response.Url, response.Fields);
    }

    public async Task<string> GeneratePresignedGetAsync(string fileKey, TimeSpan expiry)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = _options.BucketName,
            Key = fileKey,
            Expires = (_dateTimeProvider.UtcNow + expiry).UtcDateTime,
            Verb = HttpVerb.GET,
        };

        string url = await _s3Client.GetPreSignedURLAsync(request);
        return url;
    }
}
