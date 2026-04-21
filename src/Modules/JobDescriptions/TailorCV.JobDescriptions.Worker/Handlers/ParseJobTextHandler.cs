using TailorCV.JobDescriptions.Contracts.Commands;
using TailorCV.JobDescriptions.Contracts.Dto;
using TailorCV.JobDescriptions.Contracts.Events;
using TailorCV.JobDescriptions.Worker.Infrastructure.AI;

namespace TailorCV.JobDescriptions.Worker.Handlers;

public static class ParseJobTextHandler
{
    public static async Task<object> HandleAsync(
        ParseJobText command,
        IJobDescriptionParserService parser,
        CancellationToken ct)
    {
        try
        {
            ParsedJobDataDto data = await parser.ParseAsync(command.RawText, ct);
            return new JobParsingCompleted(command.ParseJobId, data, command.RawText, command.SourceUrl);
        }
        catch (Exception ex)
        {
            return new JobParsingFailed(command.ParseJobId, ex.Message);
        }
    }
}