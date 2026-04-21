namespace TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;

public sealed class PlaywrightOptions
{
    public int MaxConcurrency { get; set; } = 3;
    public int RequestTimeoutMs { get; set; } = 30000;
}