using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public static class ProfileErrors
{
    public static Error ProfileAlreadyExists =>
        Error.Conflict("PROFILE_ALREADY_EXISTS", "Profile already exists for this user");

    public static Error ProfileNotFound =>
        Error.NotFound("PROFILE_NOT_FOUND", "Profile not found");

    public static Error ParseJobNotFound =>
        Error.NotFound("PARSE_JOB_NOT_FOUND", "Parse job not found");
}
