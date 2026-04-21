using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using TailorCV.JobDescriptions.Contracts.Dto;

namespace TailorCV.JobDescriptions.Worker.Infrastructure.AI;

public sealed class OpenAiJobParserService : IJobDescriptionParserService
{
    private readonly ChatClient _client;

    public OpenAiJobParserService(OpenAIClient client, IOptions<OpenAiOptions> options)
    {
        _client = client.GetChatClient(options.Value.ModelId);
    }

    public async Task<ParsedJobDataDto> ParseAsync(string rawText, CancellationToken ct = default)
    {
        string systemPrompt = """
            You are an expert job description parser. Extract structured information from job posting text.
            Return a JSON object with these fields:
            - title: string (job title)
            - company: string (company name, or null if not found)
            - location: string (job location, or null if not found)
            - requiredSkills: string[] (list of required skills)
            - responsibilities: string[] (list of responsibilities)
            - qualifications: string[] (list of qualifications)
            - seniorityLevel: string (one of: Junior, Mid, Senior, Lead, Principal, Staff, Director)

            If a field cannot be determined, use null for nullable fields or empty array for list fields.
            """;

        string userPrompt = $"Extract job details from this job posting:\n\n{rawText}";

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            Temperature = 0.1f
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, options, ct);

        string responseText = completion.Content[0].Text.Trim();

        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseText);
            JsonElement root = doc.RootElement;

            return new ParsedJobDataDto(
                Title: root.GetProperty("title").GetString() ?? "Unknown Title",
                Company: root.TryGetProperty("company", out JsonElement c) ? c.GetString() : null,
                Location: root.TryGetProperty("location", out JsonElement l) ? l.GetString() : null,
                RequiredSkills: root.TryGetProperty("requiredSkills", out JsonElement rs)
                    ? rs.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : [],
                Responsibilities: root.TryGetProperty("responsibilities", out JsonElement r)
                    ? r.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : [],
                Qualifications: root.TryGetProperty("qualifications", out JsonElement q)
                    ? q.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : [],
                SeniorityLevel: root.TryGetProperty("seniorityLevel", out JsonElement sl)
                    ? sl.GetString() ?? "Mid"
                    : "Mid"
            );
        }
        catch
        {
            return new ParsedJobDataDto(
                Title: "Unknown Title",
                Company: null,
                Location: null,
                RequiredSkills: [],
                Responsibilities: [],
                Qualifications: [],
                SeniorityLevel: "Mid"
            );
        }
    }
}