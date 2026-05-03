 namespace TailorCV.Infrastructure.Storage;

public class BlobStorageOptions
{
    public const string SectionName = "S3";

    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
}
