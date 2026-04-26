using TailorCV.Profile.Contracts.Dto;

namespace TailorCV.Profile.Worker.Infrastructure.AI;

public interface IResumeParserService
{
    Task<ParsedResumeData> ParseAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
}
