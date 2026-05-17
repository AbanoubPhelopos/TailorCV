namespace TailorCV.CVGenerator.Worker.Infrastructure.Pdf;

public interface IPdfRenderer
{
    Task<byte[]> RenderAsync(string html, string css, CancellationToken ct = default);
}
