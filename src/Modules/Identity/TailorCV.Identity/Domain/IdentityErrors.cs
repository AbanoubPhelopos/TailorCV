using TailorCV.Shared.Results;

namespace TailorCV.Identity.Domain;

public static class IdentityErrors
{
    public static Error EmailAlreadyExists =>
        Error.Conflict("EMAIL_ALREADY_EXISTS", "A user with this email already exists");

    public static Error InvalidCredentials =>
        Error.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password");

    public static Error RefreshTokenNotFound =>
        Error.NotFound("REFRESH_TOKEN_NOT_FOUND", "Invalid or expired refresh token");

    public static Error RefreshTokenExpired =>
        Error.Unauthorized("REFRESH_TOKEN_EXPIRED", "Refresh token has expired");

    public static Error UserNotFound =>
        Error.NotFound("USER_NOT_FOUND", "User not found");

    public static Error UserDeleted =>
        Error.Unauthorized("USER_DELETED", "User account no longer exists");
}
