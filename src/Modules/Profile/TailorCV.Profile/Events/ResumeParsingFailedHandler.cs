using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Contracts.Events;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Profile.Events;

public static class ResumeParsingFailedHandler
{
    public static async Task HandleAsync(
        ResumeParsingFailed @event,
        ProfileDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        CancellationToken ct)
    {
        ParseJob? parseJob = await dbContext.ParseJobs
            .FirstOrDefaultAsync(p => p.Id == @event.ParseJobId, ct);

        if (parseJob is null)
        {
            return;
        }

        parseJob.MarkFailed(@event.Error, dateTimeProvider.UtcNow);
        await dbContext.SaveChangesAsync(ct);
    }
}
