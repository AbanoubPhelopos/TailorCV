using TailorCV.JobDescriptions.Contracts.Dto;

namespace TailorCV.JobDescriptions.Worker.Infrastructure.AI;

public interface IJobDescriptionParserService
{
    Task<ParsedJobDataDto> ParseAsync(string rawText, CancellationToken ct = default);
}