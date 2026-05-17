using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.Infrastructure.AI;

namespace TailorCV.CVGenerator.Worker.Infrastructure.AI;

public sealed class OpenAiCVTailoringService : ICVTailoringService
{
    private readonly ChatClient _client;

    public OpenAiCVTailoringService(OpenAIClient client, IOptions<OpenAiOptions> options)
    {
        _client = client.GetChatClient(options.Value.ModelId);
    }

    public async Task<string> TailorAsync(
        ProfileSnapshotData profile,
        JobSnapshotData job,
        string? tailoringPrompt,
        CancellationToken ct = default)
    {
        string systemPrompt = """
            You are an expert CV/resume tailoring assistant. Given a user's profile data and a job description,
            produce a tailored CV content as JSON with this exact structure:
            {
              "summary": "A tailored professional summary (2-3 sentences)",
              "sections": [
                {
                  "type": "Experience",
                  "title": "Relevant Experience",
                  "items": [
                    { "company": "...", "role": "...", "description": "...", "startDate": "YYYY-MM", "endDate": "YYYY-MM or null", "isCurrent": true/false }
                  ]
                },
                {
                  "type": "Skill",
                  "title": "Key Skills",
                  "items": ["skill1", "skill2"]
                },
                {
                  "type": "Education",
                  "title": "Education",
                  "items": [
                    { "institution": "...", "degree": "...", "startDate": "YYYY-MM", "endDate": "YYYY-MM" }
                  ]
                }
              ]
            }

            Rules:
            - Reorder and emphasize experience most relevant to the job description
            - Tailor the summary to highlight skills matching the job requirements
            - Include only the most relevant skills, prioritizing those in the job's required skills
            - Keep descriptions concise (1-2 sentences each)
            - Use action verbs and quantify achievements where possible
            """;

        string profileJson = JsonSerializer.Serialize(profile);
        string jobJson = JsonSerializer.Serialize(job);

        string userPrompt = $"## User Profile\n{profileJson}\n\n## Job Description\n{jobJson}";

        if (!string.IsNullOrWhiteSpace(tailoringPrompt))
        {
            userPrompt += $"\n\n## Additional Instructions\n{tailoringPrompt}";
        }

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            Temperature = 0.7f,
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, options, ct);
        return completion.Content[0].Text;
    }
}
