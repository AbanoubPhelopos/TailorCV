using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public static class ProfileErrors
{
    public static Error ProfileAlreadyExists =>
        Error.Conflict("PROFILE_ALREADY_EXISTS", "Profile already exists for this user");

    public static Error ProfileNotFound =>
        Error.NotFound("PROFILE_NOT_FOUND", "Profile not found");

    public static Error SectionNotFound =>
        Error.NotFound("SECTION_NOT_FOUND", "Section not found");

    public static Error SectionTypeMismatch =>
        Error.Validation("Section type does not match the existing section");

    public static Error SectionNotOwned =>
        Error.Forbidden("SECTION_NOT_OWNED", "You do not own this section");

    public static Error NotAllSectionsIncluded =>
        Error.Validation("Must include all sections");

    public static Error InvalidSectionIds =>
        Error.Validation("Invalid section IDs provided");

    public static Error DuplicateSectionIds =>
        Error.Validation("Duplicate section IDs are not allowed");

    public static Error NonSequentialOrders =>
        Error.Validation("Orders must be sequential starting from 1");

    public static Error ParseJobNotFound =>
        Error.NotFound("PARSE_JOB_NOT_FOUND", "Parse job not found");
}
