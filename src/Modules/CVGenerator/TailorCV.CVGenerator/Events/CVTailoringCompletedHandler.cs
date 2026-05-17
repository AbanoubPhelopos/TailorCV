using Microsoft.EntityFrameworkCore;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.Interfaces;

namespace TailorCV.CVGenerator.Events;

public static class CVTailoringCompletedHandler
{
    public static async Task HandleAsync(
        CVTailoringCompleted @event,
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
        cv.MarkDone(@event.Content, @event.MatchScore, @event.CoverLetter, now);
        await dbContext.SaveChangesAsync(ct);
    }
}
