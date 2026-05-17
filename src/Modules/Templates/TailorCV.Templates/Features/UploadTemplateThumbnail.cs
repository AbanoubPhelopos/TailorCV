#pragma warning disable CA1308
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Templates.Features;

public static class UploadTemplateThumbnail
{
    public record Request(string FileName, string ContentType);

    public record Response(string Key, string Endpoint, Dictionary<string, string> Fields);

    public class Validator : AbstractValidator<Request>
    {
        private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp"];
        private static readonly HashSet<string> AllowedContentTypes = ["image/png", "image/jpeg", "image/webp"];

        public Validator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .Must(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .WithMessage("Only PNG, JPEG, and WebP images are supported");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .Must(c => AllowedContentTypes.Contains(c))
                .WithMessage("Only PNG, JPEG, and WebP content types are supported");
        }
    }

    public class Handler(
        IBlobStorage blobStorage,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            string extension = Path.GetExtension(command.FileName);
            DateTimeOffset now = dateTimeProvider.UtcNow;
            string key = $"thumbnails/templates/{now:yyyy}/{now:MM}/{now:dd}/{Guid.CreateVersion7()}{extension}";

            PresignedPostResponse presigned = await blobStorage.GeneratePresignedPostAsync(
                key,
                command.ContentType,
                maxSizeBytes: 2 * 1024 * 1024,
                expiry: TimeSpan.FromMinutes(5),
                ct);

            Dictionary<string, string> fields = new(presigned.Fields);

            return Result<Response>.Success(new Response(key, presigned.Endpoint, fields));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/templates/upload-url", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Templates")
        .WithName("UploadTemplateThumbnail")
        .WithSummary("Get thumbnail upload URL")
        .WithDescription("Returns a presigned S3 POST URL for uploading a template thumbnail image (PNG/JPEG/WebP, max 2MB).")
        .Produces<Response>()
        .RequireAuthorization("Admin");
    }
}
