using TailorCV.CVGenerator.Contracts.Dto;

namespace TailorCV.CVGenerator.Worker.Infrastructure.Scoring;

public interface IMatchScoreCalculator
{
    MatchScoreData Calculate(ProfileSnapshotData profile, JobSnapshotData job);
}
