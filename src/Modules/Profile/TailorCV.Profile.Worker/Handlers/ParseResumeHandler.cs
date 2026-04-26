using TailorCV.Profile.Contracts.Commands;
using TailorCV.Profile.Contracts.Events;
using TailorCV.Profile.Worker.Infrastructure.AI;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Profile.Worker.Handlers;

public static class ParseResumeHandler
{
    public static async Task<object> HandleAsync(
        ParseResume command,
        IResumeParserService parser,
        IBlobStorage blobStorage,
        CancellationToken ct)
    {
        try
        {
            Stream? fileStream = await blobStorage.DownloadAsync(command.S3Key, ct);

            if (fileStream is null)
            {
                return new ResumeParsingFailed(command.ParseJobId, "Resume file not found in storage");
            }

            await using (fileStream)
            {
                Contracts.Dto.ParsedResumeData parsedData = await parser.ParseAsync(
                    fileStream, command.FileName, command.ContentType, ct);

                return new ResumeParsingCompleted(command.ParseJobId, parsedData);
            }
        }
        catch (Exception ex)
        {
            return new ResumeParsingFailed(command.ParseJobId, ex.Message);
        }
    }
}
