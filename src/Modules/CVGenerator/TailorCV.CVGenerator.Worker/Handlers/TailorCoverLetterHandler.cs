using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Worker.Infrastructure.AI;

namespace TailorCV.CVGenerator.Worker.Handlers;

public static class TailorCoverLetterHandler
{
    public static async Task<object> HandleAsync(
        Contracts.Commands.TailorCoverLetter command,
        ICoverLetterService coverLetterService,
        CancellationToken ct)
    {
        try
        {
            ProfileSnapshotData? profile = JsonSerializer.Deserialize<ProfileSnapshotData>(command.ProfileSnapshot);
            JobSnapshotData? job = JsonSerializer.Deserialize<JobSnapshotData>(command.JobSnapshot);

            if (profile is null || job is null)
            {
                return new CoverLetterFailed(command.GenerationId, "Invalid snapshot data");
            }

            string coverLetter = await coverLetterService.GenerateAsync(
                profile, job, command.TailoringPrompt, ct);

            return new CoverLetterCompleted(command.GenerationId, coverLetter);
        }
        catch (Exception ex)
        {
            return new CoverLetterFailed(command.GenerationId, ex.Message);
        }
    }
}
