#pragma warning disable CA1054, S1075

using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class ImportResumeGetUploadUrl
{
    public record Request(string FileName, string ContentType);

    public record Response(string Key, string Url, Dictionary<string, string> Fields);

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
        IConfiguration configuration,
        ICurrentUserService currentUserService) : ICommandHandler<Request, Response>
    {
        public Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;
            string extension = Path.GetExtension(command.FileName);
            string key = $"resumes/{userId}/{Guid.CreateVersion7()}{extension}";

            string bucketUrl = configuration["Storage:BucketUrl"] ?? "http://localhost:9000/tailorcv-uploads";

            Dictionary<string, string> fields = new()
            {
                ["key"] = key,
            };

            return Task.FromResult(Result<Response>.Success(new Response(key, bucketUrl, fields)));
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
        .WithDescription("Returns a presigned S3 URL for uploading a resume file.");
    }
}

#pragma warning restore CA1054, S1075
