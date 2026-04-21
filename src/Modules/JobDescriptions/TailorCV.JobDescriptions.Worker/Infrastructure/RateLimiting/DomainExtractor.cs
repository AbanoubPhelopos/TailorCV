namespace TailorCV.JobDescriptions.Worker.Infrastructure.RateLimiting;

public static class DomainExtractor
{
    #pragma warning disable CA1308
    public static string Extract(Uri url)
    {
        return url.Host.ToLowerInvariant();
    }
    #pragma warning restore CA1308
}