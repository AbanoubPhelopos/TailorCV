using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Domain.Enums;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.JobDescriptions.Contracts.Commands;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.JobDescriptions.Features;

public static class ScrapeJobDescription
{
    public record Request(Uri SourceUrl);

    public record Response(Guid ParseId);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SourceUrl)
                .NotEmpty()
                .Must(uri => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                .WithMessage("Must be a valid HTTP/HTTPS URL");
        }
    }

    public class Handler(
        JobDescriptionsDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            ParseJob parseJob = ParseJob.Create(
                currentUser.UserId,
                ParseJobType.UrlScrape,
                string.Empty,
                command.SourceUrl,
                dateTimeProvider.UtcNow);

            dbContext.ParseJobs.Add(parseJob);
            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new ScrapeJobUrl(parseJob.Id, command.SourceUrl));

            return Result<Response>.Success(new Response(parseJob.Id));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/jobs/scrape", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/jobs/parse/{result.Value.ParseId}/status", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("JobDescription")
        .WithName("ScrapeJobDescription")
        .WithSummary("Scrape a job posting from URL")
        .WithDescription("Uses Playwright to scrape a job posting URL, then AI parses it. Poll status for results.")
        .RequireAuthorization();
    }
}
