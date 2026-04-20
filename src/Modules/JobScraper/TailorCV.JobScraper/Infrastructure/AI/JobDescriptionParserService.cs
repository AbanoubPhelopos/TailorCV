using System.Text.Json;
using TailorCV.JobScraper.Domain;

namespace TailorCV.JobScraper.Infrastructure.AI;

public interface IJobDescriptionParserService
{
    Task<ParsedJobData> ParseAsync(string rawText, CancellationToken ct = default);
}

public sealed class JobDescriptionParserService : IJobDescriptionParserService
{
    public async Task<ParsedJobData> ParseAsync(string rawText, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);

        return new ParsedJobData(
            Title: ExtractTitle(rawText),
            Company: null,
            Location: null,
            RequiredSkills: ExtractSkills(rawText),
            Responsibilities: [],
            Qualifications: [],
            SeniorityLevel: "Mid"
        );
    }

    private static string ExtractTitle(string text)
    {
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? lines[0].Trim() : "Unknown Title";
    }

    private static List<string> ExtractSkills(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Take(5).ToList();
    }
}
