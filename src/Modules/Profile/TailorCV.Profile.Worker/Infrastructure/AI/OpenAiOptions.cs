using System.ComponentModel.DataAnnotations;

namespace TailorCV.Profile.Worker.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Url]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ModelId { get; set; } = "gpt-4o-mini";

    [Range(1, 100000)]
    public int MaxTokens { get; set; } = 4096;
}
