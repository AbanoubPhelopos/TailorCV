using TailorCV.CVGenerator.Contracts.Dto;

namespace TailorCV.CVGenerator.Worker.Infrastructure.AI;

public interface ICoverLetterService
{
    Task<string> GenerateAsync(
        ProfileSnapshotData profile,
        JobSnapshotData job,
        string? tailoringPrompt,
        CancellationToken ct = default);
}
