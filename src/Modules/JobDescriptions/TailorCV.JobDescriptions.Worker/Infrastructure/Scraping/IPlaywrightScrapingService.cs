namespace TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;

public interface IPlaywrightScrapingService
{
    Task<string> ScrapeAsync(Uri url, CancellationToken ct = default);
}