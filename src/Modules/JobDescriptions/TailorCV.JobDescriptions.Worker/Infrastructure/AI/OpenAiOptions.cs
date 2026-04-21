namespace TailorCV.JobDescriptions.Worker.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 2048;
}