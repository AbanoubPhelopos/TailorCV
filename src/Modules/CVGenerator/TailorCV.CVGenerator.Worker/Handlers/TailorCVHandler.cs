using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Worker.Infrastructure.AI;
using TailorCV.CVGenerator.Worker.Infrastructure.Scoring;

namespace TailorCV.CVGenerator.Worker.Handlers;

public static class TailorCVHandler
{
    public static async Task<object> HandleAsync(
        Contracts.Commands.TailorCV command,
        ICVTailoringService tailoringService,
        ICoverLetterService coverLetterService,
        IMatchScoreCalculator matchScoreCalculator,
        CancellationToken ct)
    {
        try
        {
            ProfileSnapshotData? profile = JsonSerializer.Deserialize<ProfileSnapshotData>(command.ProfileSnapshot);
            JobSnapshotData? job = JsonSerializer.Deserialize<JobSnapshotData>(command.JobSnapshot);

            if (profile is null || job is null)
            {
                return new CVTailoringFailed(command.GenerationId, "Invalid snapshot data");
            }

            MatchScoreData matchScore = matchScoreCalculator.Calculate(profile, job);
            string matchScoreJson = JsonSerializer.Serialize(matchScore);

            string contentJson = await tailoringService.TailorAsync(profile, job, command.TailoringPrompt, ct);

            string? coverLetter = null;
            if (command.IncludeCoverLetter)
            {
                coverLetter = await coverLetterService.GenerateAsync(profile, job, command.TailoringPrompt, ct);
            }

            return new CVTailoringCompleted(command.GenerationId, contentJson, matchScoreJson, coverLetter);
        }
        catch (Exception ex)
        {
            return new CVTailoringFailed(command.GenerationId, ex.Message);
        }
    }
}
