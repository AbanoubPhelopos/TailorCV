using System.ComponentModel.DataAnnotations;

namespace TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;

public sealed class PlaywrightOptions
{
    public const string SectionName = "Playwright";

    [Range(1, 100)]
    public int MaxConcurrency { get; set; } = 3;

    [Range(1000, 600000)]
    public int RequestTimeoutMs { get; set; } = 30000;
}
