using Microsoft.EntityFrameworkCore;
using TailorCV.JobDescriptions.Contracts.Events;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.Interfaces;

namespace TailorCV.JobDescriptions.Events;

public static class JobParsingFailedHandler
{
    public static async Task HandleAsync(
        JobParsingFailed @event,
        JobDescriptionsDbContext dbContext,
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