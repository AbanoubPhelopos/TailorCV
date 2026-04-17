using Microsoft.AspNetCore.Http;

namespace TailorCV.Shared.Results;

public static class ResultExtensions
{
    public static IResult ToProblemDetails<T>(this Result<T> result)
    {
        return global::Microsoft.AspNetCore.Http.Results.Json(
            new { code = result.Error.Code, message = result.Error.Message },
            statusCode: result.Error.Type.ToHttpStatusCode());
    }

    public static IResult ToProblemDetails(this Result result)
    {
        return global::Microsoft.AspNetCore.Http.Results.Json(
            new { code = result.Error.Code, message = result.Error.Message },
            statusCode: result.Error.Type.ToHttpStatusCode());
    }
}
