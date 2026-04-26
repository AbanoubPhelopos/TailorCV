using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using TailorCV.Profile.Contracts.Dto;

namespace TailorCV.Profile.Worker.Infrastructure.AI;

public sealed class OpenAiResumeParserService : IResumeParserService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ChatClient _client;

    public OpenAiResumeParserService(OpenAIClient client, IOptions<OpenAiOptions> options)
    {
        _client = client.GetChatClient(options.Value.ModelId);
    }

    public async Task<ParsedResumeData> ParseAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        string systemPrompt = """
            You are an expert resume parser. Extract structured information from the provided resume document.
            Return a JSON object with these top-level fields:
            - headline: string (professional headline/title, or null)
            - summary: string (professional summary, or null)
            - phone: string (phone number, or null)
            - location: string (location, or null)
            - website: string (website URL, or null)
            - linkedin: string (LinkedIn URL, or null)
            - github: string (GitHub URL, or null)
            - sections: array of section objects, each with:
              - type: string (one of "experience", "skill", "education", "project", "certification", "language", "custom")
              - order: integer (starting from 1)
              - items: array of items for this section

            Item fields by type:
            - experience: { order, company, role, startDate (YYYY-MM-DD), endDate (YYYY-MM-DD or null), description, isCurrent }
            - project: { order, name, description, techStack (string[]), role, url, startDate, endDate }
            - skill: { order, name }
            - education: { order, institution, degree, field, startDate, endDate, gpa }
            - certification: { order, name, issuer, date, expiryDate, url }
            - language: { order, languageName, proficiency }
            - custom: { order, title, subtitle, description }

            If a field cannot be determined, use null for nullable fields or empty array for list fields.
            Only include sections that have meaningful content from the resume.
            """;

        using MemoryStream ms = new();
        await fileStream.CopyToAsync(ms, ct);
        BinaryData fileData = BinaryData.FromBytes(ms.ToArray());

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart("Extract structured data from this resume:"),
#pragma warning disable OPENAI001
                ChatMessageContentPart.CreateFilePart(fileData, contentType, fileName)),
#pragma warning restore OPENAI001
        ];

        ChatCompletionOptions chatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            Temperature = 0.1f,
        };

        ChatCompletion completion = await _client.CompleteChatAsync(messages, chatOptions, ct);

        string responseText = completion.Content[0].Text.Trim();

        return JsonSerializer.Deserialize<ParsedResumeData>(responseText, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse resume data");
    }
}
