using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace TailorCV.JobDescriptions.Worker.Infrastructure.RateLimiting;

public sealed class DomainRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucketRateLimiter> _limiters = new();
    private readonly TimeSpan _replenishmentPeriod;
    private readonly int _tokensPerPeriod;
    private readonly int _bucketSize;

    public DomainRateLimiter(
        TimeSpan? replenishmentPeriod = null,
        int? tokensPerPeriod = null,
        int? bucketSize = null)
    {
        _replenishmentPeriod = replenishmentPeriod ?? TimeSpan.FromSeconds(5);
        _tokensPerPeriod = tokensPerPeriod ?? 1;
        _bucketSize = bucketSize ?? 2;
    }

    public async Task<RateLimitLease> AcquireAsync(string domain, CancellationToken ct = default)
    {
        TokenBucketRateLimiter limiter = _limiters.GetOrAdd(
            domain,
            _ => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = _bucketSize,
                ReplenishmentPeriod = _replenishmentPeriod,
                TokensPerPeriod = _tokensPerPeriod,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

        return await limiter.AcquireAsync(1, ct);
    }
}