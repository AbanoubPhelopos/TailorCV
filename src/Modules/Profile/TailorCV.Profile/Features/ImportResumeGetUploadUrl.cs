using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class ImportResumeGetUploadUrl
{
    public record Request(string FileName, string ContentType);

    public record Response(string Key, string Endpoint, Dictionary<string, string> Fields);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only PDF and DOCX files are supported");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .Must(c => c is "application/pdf" or
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                .WithMessage("Only PDF and DOCX content types are supported");
        }
    }

    public class Handler(
        IBlobStorage blobStorage,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;
            string extension = Path.GetExtension(command.FileName);
            DateTimeOffset now = dateTimeProvider.UtcNow;
            string key = $"resumes/{userId}/{now:yyyy}/{now:MM}/{now:dd}/{Guid.CreateVersion7()}{extension}";

            PresignedPostResponse presigned = await blobStorage.GeneratePresignedPostAsync(
                key,
                command.ContentType,
                maxSizeBytes: 5 * 1024 * 1024,
                expiry: TimeSpan.FromMinutes(5),
                ct);

            Dictionary<string, string> fields = new(presigned.Fields);

            return Result<Response>.Success(new Response(key, presigned.Endpoint, fields));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles/me/import/upload-url", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("ImportResumeGetUploadUrl")
        .WithSummary("Get presigned upload URL")
        .WithDescription("Returns a presigned S3 POST URL and fields for uploading a resume file.")
        .Produces<Response>();
    }
}
