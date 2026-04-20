using TailorCV.Shared.Results;

namespace TailorCV.JobScraper.Domain;

public static class JobScraperErrors
{
    public static Error ParseJobNotFound =>
        Error.NotFound("PARSE_JOB_NOT_FOUND", "Parse job not found");

    public static Error JobDescriptionNotFound =>
        Error.NotFound("JOB_NOT_FOUND", "Job description not found");

    public static Error NotOwner =>
        Error.Forbidden("NOT_OWNER", "You do not have access to this resource");
}
