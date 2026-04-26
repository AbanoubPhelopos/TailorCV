using TailorCV.JobDescriptions.Contracts.Commands;
using TailorCV.JobDescriptions.Contracts.Events;
using TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;

namespace TailorCV.JobDescriptions.Worker.Handlers;

public static class ScrapeJobUrlHandler
{
    public static async Task<object> HandleAsync(
        ScrapeJobUrl command,
        IPlaywrightScrapingService scraper,
        CancellationToken ct)
    {
        try
        {
            string rawText = await scraper.ScrapeAsync(command.SourceUrl, ct);
            return new ParseJobText(command.ParseJobId, rawText, command.SourceUrl);
        }
        catch (Exception ex)
        {
            return new JobParsingFailed(command.ParseJobId, ex.Message);
        }
    }
}
