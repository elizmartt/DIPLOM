using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

namespace ApiGateway.Services;

public class S3Service
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IAmazonS3 s3, IConfiguration config, ILogger<S3Service> logger)
    {
        _s3     = s3;
        _bucket = config["S3_BUCKET_NAME"]!;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(
        Stream fileStream, string caseId, string fileName, string contentType)
    {
        var key = $"patients/{caseId}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        var request = new TransferUtilityUploadRequest
        {
            BucketName  = _bucket,
            Key         = key,
            InputStream = fileStream,
            ContentType = contentType
        };

        var transfer = new TransferUtility(_s3);
        await transfer.UploadAsync(request);

        _logger.LogInformation(" Uploaded image to S3: {Key}", key);
        return key;
    }


    public async Task<byte[]> DownloadImageAsync(string s3Key)
    {
        _logger.LogInformation("⬇ Downloading from S3: {Key}", s3Key);

        var response = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucket,
            Key        = s3Key
        });

        using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms);
        return ms.ToArray();
    }

    public string GetPresignedUrl(string s3Key, int expiryMinutes = 60)
    {
        return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key        = s3Key,
            Expires    = DateTime.UtcNow.AddMinutes(expiryMinutes)
        });
    }
}