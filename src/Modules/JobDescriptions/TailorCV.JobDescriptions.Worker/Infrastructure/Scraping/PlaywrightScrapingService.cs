using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using TailorCV.JobDescriptions.Worker.Infrastructure.RateLimiting;

namespace TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;

public sealed class PlaywrightScrapingService : IPlaywrightScrapingService, IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly DomainRateLimiter _domainRateLimiter;
    private readonly TimeSpan _requestTimeout;
    private bool _disposed;

    public PlaywrightScrapingService(
        IOptions<PlaywrightOptions> options,
        DomainRateLimiter domainRateLimiter)
    {
        _concurrencyLimiter = new SemaphoreSlim(options.Value.MaxConcurrency);
        _domainRateLimiter = domainRateLimiter;
        _requestTimeout = TimeSpan.FromMilliseconds(options.Value.RequestTimeoutMs);

        IPlaywright playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
        _playwright = playwright;

        IBrowser browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        }).GetAwaiter().GetResult();

        _browser = browser;
    }

    public async Task<string> ScrapeAsync(Uri url, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            string domain = DomainExtractor.Extract(url);
            using RateLimitLease lease = await _domainRateLimiter.AcquireAsync(domain, ct);

            if (!lease.IsAcquired)
            {
                if (attempt < maxRetries)
                {
                    await Task.Delay(500, ct);
                    continue;
                }
                throw new InvalidOperationException($"Rate limit exceeded for domain after {maxRetries} attempts: {domain}");
            }

            await _concurrencyLimiter.WaitAsync(ct);
            try
            {
                return await ScrapePageAsync(url, ct);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        return await ScrapePageAsync(url, ct);
    }

    private async Task<string> ScrapePageAsync(Uri url, CancellationToken ct)
    {
        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US"
        });

        try
        {
            IPage page = await context.NewPageAsync();

            await page.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                delete navigator.__proto__?.webdriver;
            ");

            await page.GotoAsync(url.ToString(), new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = (int)_requestTimeout.TotalMilliseconds
            });

            int delayMs = GetSecureRandomInt(1000, 3000);
            await Task.Delay(delayMs, ct);

            IElementHandle? mainContent = await page.QuerySelectorAsync("article, main, [role='main'], .job-content, .job-description, #job-content");
            string content = mainContent is not null
                ? await mainContent.InnerTextAsync()
                : await page.ContentAsync();

            content = CleanHtml(content);
            return content;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private static int GetSecureRandomInt(int minValue, int maxValue)
    {
        int value = RandomNumberGenerator.GetInt32(minValue, maxValue);
        return value;
    }

    private static string CleanHtml(string html)
    {
        return Regex.Replace(html
            .Replace(Environment.NewLine, " ")
            .Replace("\n", " ")
            .Replace("\r", " "), @"\s+", " ").Trim();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _browser.CloseAsync();
        _playwright.Dispose();
        _concurrencyLimiter.Dispose();
    }
}