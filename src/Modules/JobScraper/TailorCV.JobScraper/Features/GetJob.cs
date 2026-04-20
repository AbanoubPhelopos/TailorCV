using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Domain.Enums;
using TailorCV.JobScraper.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.JobScraper.Features;

public static class GetJob
{
    public record Request(Guid Id);

    public record Response(
        Guid Id,
        string Title,
        string Company,
        string? Location,
        List<string> RequiredSkills,
        List<string> Responsibilities,
        List<string> Qualifications,
        SeniorityLevel? SeniorityLevel,
        Uri? SourceUrl,
        string? Label,
        string? RawText,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public class Handler(JobScraperDbContext dbContext, ICurrentUserService currentUser)
        : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            JobDescription? job = await dbContext.JobDescriptions
                .FirstOrDefaultAsync(j => j.Id == query.Id, ct);

            if (job is null)
            {
                return Result<Response>.Failure(JobScraperErrors.JobDescriptionNotFound);
            }

            if (!job.IsOwner(currentUser.UserId))
            {
                return Result<Response>.Failure(JobScraperErrors.NotOwner);
            }

            return Result<Response>.Success(new Response(
                job.Id,
                job.Title,
                job.Company,
                job.Location,
                job.RequiredSkills,
                job.Responsibilities,
                job.Qualifications,
                job.SeniorityLevel,
                job.SourceUrl is null ? null : new Uri(job.SourceUrl),
                job.Label,
                job.RawText,
                job.CreatedAt,
                job.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/jobs/{id}", async (
            Guid id,
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("JobScraper")
        .WithName("GetJob")
        .WithSummary("Get a saved job description")
        .WithDescription("Returns full details of a saved job description.")
        .RequireAuthorization();
    }
}
