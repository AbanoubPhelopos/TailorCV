using TailorCV.Shared.Results;

namespace TailorCV.CVGenerator.Domain;

public static class CVErrors
{
    public static Error CVNotFound =>
        Error.NotFound("CV_NOT_FOUND", "Generated CV not found");

    public static Error CVStillProcessing =>
        Error.Conflict("CV_STILL_PROCESSING", "CV generation is still in progress");

    public static Error CVContentNotReady =>
        Error.Conflict("CV_CONTENT_NOT_READY", "CV content is not ready yet");

    public static Error PdfNotReady =>
        Error.NotFound("PDF_NOT_READY", "PDF not ready yet");

    public static Error ProfileNotFound =>
        Error.NotFound("PROFILE_NOT_FOUND", "Profile not found");

    public static Error JobNotFound =>
        Error.NotFound("JOB_NOT_FOUND", "Job description not found");
}
