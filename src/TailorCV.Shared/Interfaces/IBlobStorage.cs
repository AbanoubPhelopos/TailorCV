namespace TailorCV.Shared.Interfaces;

public interface IBlobStorage
{
    Task InitializeAsync(CancellationToken ct = default);

    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);

    Task<Stream?> DownloadAsync(string fileKey, CancellationToken ct = default);

    Task DeleteAsync(string fileKey, CancellationToken ct = default);

    string GetPublicLink(string fileKey);

    Task MoveAsync(string sourceKey, string destinationKey, CancellationToken ct = default);

    Task<bool> ExistsAsync(string fileKey, CancellationToken ct = default);

    Task<PresignedPostResponse> GeneratePresignedPostAsync(
        string fileKey,
        string contentType,
        long maxSizeBytes,
        TimeSpan expiry,
        CancellationToken ct = default);

    Task<string> GeneratePresignedGetAsync(string fileKey, TimeSpan expiry);
}

public record PresignedPostResponse(
    string Endpoint,
    IDictionary<string, string> Fields);
