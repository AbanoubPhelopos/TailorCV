using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Domain.Enums;
using TailorCV.JobScraper.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.JobScraper.Features;

public static class ScrapeJobUrl
{
    public record Request(Uri Url);

    public record Response(Guid ParseId);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .Must(uri => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                .WithMessage("Must be a valid HTTP/HTTPS URL");
        }
    }

    public class Handler(
        JobScraperDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            ParseJob parseJob = ParseJob.Create(
                currentUser.UserId,
                ParseJobType.UrlScrape,
                command.Url.ToString(),
                dateTimeProvider.UtcNow);

            dbContext.ParseJobs.Add(parseJob);
            await dbContext.SaveChangesAsync(ct);

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
        .WithTags("JobScraper")
        .WithName("ScrapeJobUrl")
        .WithSummary("Scrape a job posting from URL")
        .WithDescription("Uses Playwright to scrape a job posting URL, then AI parses it. Poll status for results.")
        .RequireAuthorization();
    }
}
