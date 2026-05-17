using Microsoft.EntityFrameworkCore;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.Interfaces;

namespace TailorCV.CVGenerator.Events;

public static class CvPdfExportCompletedHandler
{
    public static async Task HandleAsync(
        CvPdfExportCompleted @event,
        CVGeneratorDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        CancellationToken ct)
    {
        Domain.GeneratedCV? cv = await dbContext.GeneratedCVs
            .FirstOrDefaultAsync(c => c.Id == @event.GenerationId, ct);

        if (cv is null)
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        cv.MarkPdfReady(@event.PdfKey, now);
        await dbContext.SaveChangesAsync(ct);
    }
}
