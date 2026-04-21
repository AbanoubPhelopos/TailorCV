using Microsoft.EntityFrameworkCore;
using TailorCV.JobDescriptions.Contracts.Events;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.Interfaces;

namespace TailorCV.JobDescriptions.Events;

public static class JobParsingCompletedHandler
{
    public static async Task HandleAsync(
        JobParsingCompleted @event,
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

        ParsedJobData parsedData = new(
            @event.Data.Title,
            @event.Data.Company,
            @event.Data.Location,
            @event.Data.RequiredSkills,
            @event.Data.Responsibilities,
            @event.Data.Qualifications,
            @event.Data.SeniorityLevel);

        parseJob.MarkDone(parsedData, dateTimeProvider.UtcNow);
        await dbContext.SaveChangesAsync(ct);
    }
}