using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.Infrastructure.AI;

namespace TailorCV.CVGenerator.Worker.Infrastructure.AI;

public sealed class OpenAiCoverLetterService : ICoverLetterService
{
    private readonly ChatClient _client;

    public OpenAiCoverLetterService(OpenAIClient client, IOptions<OpenAiOptions> options)
    {
        _client = client.GetChatClient(options.Value.ModelId);
    }

    public async Task<string> GenerateAsync(
        ProfileSnapshotData profile,
        JobSnapshotData job,
        string? tailoringPrompt,
        CancellationToken ct = default)
    {
        string systemPrompt = """
            You are an expert cover letter writer. Given a user's profile data and a job description,
            write a compelling, professional cover letter.

            Rules:
            - Start with "Dear Hiring Manager,"
            - 3-4 paragraphs maximum
            - Opening: express interest and mention the specific role
            - Body: highlight 2-3 most relevant experiences/skills matching the job
            - Closing: express enthusiasm and request an interview
            - Professional but genuine tone
            - Do not use placeholder text — write the actual letter
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
            Temperature = 0.7f,
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, options, ct);
        return completion.Content[0].Text;
    }
}
