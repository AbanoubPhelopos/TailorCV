namespace TailorCV.JobScraper.Infrastructure.Scraping;

public interface IPlaywrightScrapingService
{
    Task<string> ScrapeAsync(Uri url, CancellationToken ct = default);
}

public sealed class PlaywrightScrapingService : IPlaywrightScrapingService
{
    public async Task<string> ScrapeAsync(Uri url, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return $"Scraped content from {url}";
    }
}
