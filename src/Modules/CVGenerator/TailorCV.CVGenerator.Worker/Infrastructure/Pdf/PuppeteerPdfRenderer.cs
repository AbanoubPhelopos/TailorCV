using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace TailorCV.CVGenerator.Worker.Infrastructure.Pdf;

public sealed class PuppeteerPdfRenderer : IPdfRenderer, IDisposable
{
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<byte[]> RenderAsync(string html, string css, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _browser ??= await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
            });

            await using IPage page = await _browser.NewPageAsync();

            string fullHtml = html.Replace("</head>", $"<style>{css}</style></head>");

            await page.SetContentAsync(fullHtml, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0]
            });

            byte[] pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "0",
                    Bottom = "0",
                    Left = "0",
                    Right = "0"
                }
            });

            return pdfBytes;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _browser?.Dispose();
        _lock.Dispose();
    }
}
