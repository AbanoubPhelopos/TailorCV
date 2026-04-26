using TailorCV.Shared.Results;

namespace TailorCV.JobDescriptions.Domain;

public static class JobDescriptionErrors
{
    public static Error ParseJobNotFound =>
        Error.NotFound("PARSE_JOB_NOT_FOUND", "Parse job not found");

    public static Error JobDescriptionNotFound =>
        Error.NotFound("JOB_NOT_FOUND", "Job description not found");

    public static Error NotOwner =>
        Error.Forbidden("NOT_OWNER", "You do not have access to this resource");
}
