using Microsoft.EntityFrameworkCore;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.Interfaces;

namespace TailorCV.CVGenerator.Events;

public static class CvPdfExportFailedHandler
{
    public static async Task HandleAsync(
        CvPdfExportFailed @event,
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
        cv.MarkPdfFailed(now);
        await dbContext.SaveChangesAsync(ct);
    }
}
