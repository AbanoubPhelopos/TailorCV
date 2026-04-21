using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Domain.Enums;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.JobDescriptions.Features;

public static class SaveJobDescription
{
    public record Request(
        string Title,
        string Company,
        string? Location,
        List<string>? RequiredSkills,
        List<string>? Responsibilities,
        List<string>? Qualifications,
        SeniorityLevel? SeniorityLevel,
        Uri? SourceUrl,
        string? Label,
        string? RawText);

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
        DateTimeOffset CreatedAt);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Location).MaximumLength(200).When(l => l.Location is not null);
            RuleFor(x => x.RequiredSkills).Must(s => s is null || s.Count <= 30);
            RuleFor(x => x.Responsibilities).Must(r => r is null || r.Count <= 20);
            RuleFor(x => x.Qualifications).Must(q => q is null || q.Count <= 20);
            RuleFor(x => x.SourceUrl).Must(u => u is null || u.ToString().Length <= 2048).When(u => u is not null);
            RuleFor(x => x.Label).MaximumLength(100).When(l => l is not null);
            RuleFor(x => x.RawText).MaximumLength(10000).When(t => t is not null);
        }
    }

    public class Handler(
        JobDescriptionsDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            JobDescription job = JobDescription.Create(
                currentUser.UserId,
                command.Title,
                command.Company,
                command.Location,
                command.RequiredSkills,
                command.Responsibilities,
                command.Qualifications,
                command.SeniorityLevel,
                command.SourceUrl,
                command.Label,
                command.RawText,
                dateTimeProvider.UtcNow);

            dbContext.JobDescriptions.Add(job);
            await dbContext.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                job.Id,
                job.Title,
                job.Company,
                job.Location,
                job.RequiredSkills,
                job.Responsibilities,
                job.Qualifications,
                job.SeniorityLevel,
                job.SourceUrl,
                job.Label,
                job.CreatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/jobs", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/jobs/{result.Value.Id}", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("JobDescription")
        .WithName("SaveJobDescription")
        .WithSummary("Save a parsed job description")
        .WithDescription("Saves a job description to the database for later reuse.")
        .RequireAuthorization();
    }
}
