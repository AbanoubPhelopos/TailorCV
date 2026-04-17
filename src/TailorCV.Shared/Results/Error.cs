namespace TailorCV.Shared.Results;

public enum ErrorType
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
}

public record Error(string Code, string Message, ErrorType Type)
{
    public static Error None => new(string.Empty, string.Empty, ErrorType.None);

    public static Error Validation(string message) =>
        new("VALIDATION", message, ErrorType.Validation);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);
}

public static class ErrorTypeExtensions
{
    public static int ToHttpStatusCode(this ErrorType type) => type switch
    {
        ErrorType.None => 200,
        ErrorType.Validation => 400,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        _ => 500,
    };
}
