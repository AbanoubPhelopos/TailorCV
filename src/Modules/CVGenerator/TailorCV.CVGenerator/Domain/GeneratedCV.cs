using TailorCV.CVGenerator.Domain;
using TailorCV.Shared.Primitives;

namespace TailorCV.CVGenerator.Domain;

public class GeneratedCV : Entity
{
    public Guid UserId { get; private set; }
    public string ProfileSnapshot { get; private set; } = string.Empty;
    public string JobSnapshot { get; private set; } = string.Empty;
    public Guid TemplateId { get; private set; }
    public string? Content { get; private set; }
    public string? MatchScore { get; private set; }
    public string? CoverLetter { get; private set; }
    public GenerationType GenerationType { get; private set; }
    public string? TailoringPrompt { get; private set; }
    public GenerationStatus Status { get; private set; }
    public string? Error { get; private set; }
    public string? PdfKey { get; private set; }
    public PdfStatus PdfStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private GeneratedCV() { }

    public static GeneratedCV Create(
        Guid userId,
        string profileSnapshot,
        string jobSnapshot,
        Guid templateId,
        GenerationType generationType,
        string? tailoringPrompt,
        DateTimeOffset now)
    {
        return new GeneratedCV
        {
            UserId = userId,
            ProfileSnapshot = profileSnapshot,
            JobSnapshot = jobSnapshot,
            TemplateId = templateId,
            GenerationType = generationType,
            TailoringPrompt = tailoringPrompt,
            Status = GenerationStatus.Queued,
            PdfStatus = PdfStatus.None,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = GenerationStatus.Processing;
        UpdatedAt = now;
    }

    public void MarkDone(string content, string matchScore, string? coverLetter, DateTimeOffset now)
    {
        Content = content;
        MatchScore = matchScore;
        CoverLetter = coverLetter;
        Status = GenerationStatus.Done;
        Error = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = GenerationStatus.Failed;
        Error = error;
        UpdatedAt = now;
    }

    public void UpdateContent(string content, DateTimeOffset now)
    {
        Content = content;
        PdfKey = null;
        PdfStatus = PdfStatus.None;
        UpdatedAt = now;
    }

    public void SetCoverLetter(string coverLetter, DateTimeOffset now)
    {
        CoverLetter = coverLetter;
        UpdatedAt = now;
    }

    public void StartPdfExport(DateTimeOffset now)
    {
        PdfStatus = PdfStatus.Pending;
        UpdatedAt = now;
    }

    public void MarkPdfReady(string pdfKey, DateTimeOffset now)
    {
        PdfKey = pdfKey;
        PdfStatus = PdfStatus.Ready;
        UpdatedAt = now;
    }

    public void MarkPdfFailed(DateTimeOffset now)
    {
        PdfStatus = PdfStatus.Failed;
        UpdatedAt = now;
    }

    public void MarkCoverLetterFailed(string error, DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}
