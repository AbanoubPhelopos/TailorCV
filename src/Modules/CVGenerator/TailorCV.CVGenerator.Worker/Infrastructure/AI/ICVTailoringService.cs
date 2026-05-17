using TailorCV.CVGenerator.Contracts.Dto;

namespace TailorCV.CVGenerator.Worker.Infrastructure.AI;

public interface ICVTailoringService
{
    Task<string> TailorAsync(
        ProfileSnapshotData profile,
        JobSnapshotData job,
        string? tailoringPrompt,
        CancellationToken ct = default);
}
