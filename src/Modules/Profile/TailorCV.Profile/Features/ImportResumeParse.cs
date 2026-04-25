using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class ImportResumeParse
{
    public record Request(string Key);

    public record ParseTriggerResponse(Guid ParseId);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .Must(k => k.StartsWith("resumes/", StringComparison.Ordinal))
                .WithMessage("Invalid S3 key format");
        }
    }

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, ParseTriggerResponse>
    {
        public async Task<Result<ParseTriggerResponse>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            string expectedPrefix = $"resumes/{userId}/";
            if (!command.Key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ParseTriggerResponse>.Failure(Error.Validation("S3 key does not belong to this user"));
            }

            ParseJob parseJob = ParseJob.Create(userId, command.Key, dateTimeProvider.UtcNow);
            dbContext.ParseJobs.Add(parseJob);
            await dbContext.SaveChangesAsync(ct);

            return Result<ParseTriggerResponse>.Success(new ParseTriggerResponse(parseJob.Id));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles/me/import/parse", async (
            Request request,
            ICommandHandler<Request, ParseTriggerResponse> handler,
            CancellationToken ct) =>
        {
            Result<ParseTriggerResponse> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Json(result.Value, statusCode: 202)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("ImportResumeParse")
        .WithSummary("Trigger resume parsing")
        .WithDescription("Triggers async AI parsing of an uploaded resume. Returns a parseId for polling.");
    }
}
