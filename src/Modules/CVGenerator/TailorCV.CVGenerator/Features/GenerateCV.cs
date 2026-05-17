#pragma warning disable CA1054
using FluentValidation;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Commands;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.CVGenerator.Domain;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.JobDescriptions.Contracts.Grpc;
using TailorCV.Profile.Contracts.Grpc;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.CVGenerator.Features;

public static class GenerateCV
{
    public record Request(
        Guid ProfileId,
        Guid JobId,
        Guid TemplateId,
        bool IncludeCoverLetter,
        string? TailoringPrompt);

    public record Response(Guid GenerationId);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProfileId).NotEmpty();
            RuleFor(x => x.JobId).NotEmpty();
            RuleFor(x => x.TemplateId).NotEmpty();
            RuleFor(x => x.TailoringPrompt)
                .MaximumLength(2000)
                .When(x => x.TailoringPrompt is not null);
        }
    }

    public class Handler(
        CVGeneratorDbContext dbContext,
        ProfileService.ProfileServiceClient profileClient,
        JobDescriptionsService.JobDescriptionsServiceClient jobClient,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GetProfileByIdResponse profileResponse;
            try
            {
                profileResponse = await profileClient.GetProfileByIdAsync(
                    new GetProfileByIdRequest { Id = command.ProfileId.ToString() },
                    cancellationToken: ct);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return Result<Response>.Failure(CVErrors.ProfileNotFound);
            }

            GetJobByIdResponse jobResponse;
            try
            {
                jobResponse = await jobClient.GetJobByIdAsync(
                    new GetJobByIdRequest { Id = command.JobId.ToString() },
                    cancellationToken: ct);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return Result<Response>.Failure(CVErrors.JobNotFound);
            }

            if (jobResponse.UserId != userId.ToString())
            {
                return Result<Response>.Failure(CVErrors.JobNotFound);
            }

            string profileSnapshot = SerializeProfile(profileResponse);
            string jobSnapshot = SerializeJob(jobResponse);

            DateTimeOffset now = dateTimeProvider.UtcNow;

            GeneratedCV generatedCV = GeneratedCV.Create(
                userId,
                profileSnapshot,
                jobSnapshot,
                command.TemplateId,
                GenerationType.FullCV,
                command.TailoringPrompt,
                now);

            dbContext.GeneratedCVs.Add(generatedCV);
            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new Contracts.Commands.TailorCV(
                generatedCV.Id,
                userId,
                profileSnapshot,
                jobSnapshot,
                command.TemplateId,
                command.IncludeCoverLetter,
                command.TailoringPrompt));

            return Result<Response>.Success(new Response(generatedCV.Id));
        }

        private static string SerializeProfile(GetProfileByIdResponse profile)
        {
            List<ProfileSectionSnapshot>? sections = null;
            if (!string.IsNullOrEmpty(profile.SectionsJson))
            {
                sections = JsonSerializer.Deserialize<List<ProfileSectionSnapshot>>(profile.SectionsJson);
            }

            ProfileSnapshotData snapshot = new(
                profile.Headline,
                profile.Summary,
                string.Empty,
                string.Empty,
                string.Empty,
                profile.Phone,
                profile.Location,
                sections ?? []);

            return JsonSerializer.Serialize(snapshot);
        }

        private static string SerializeJob(GetJobByIdResponse job)
        {
            JobSnapshotData snapshot = new(
                job.Title,
                job.Company,
                job.Location,
                job.RequiredSkills.ToList(),
                job.Responsibilities.ToList(),
                job.Qualifications.ToList(),
                job.SeniorityLevel);

            return JsonSerializer.Serialize(snapshot);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cv/generate", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/cv/generate/{result.Value.GenerationId}/status", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("GenerateCV")
        .WithSummary("Generate a tailored CV")
        .WithDescription("Combines profile, job description, and template to generate an AI-tailored CV. Async operation — poll status for results.")
        .Produces<Response>(202)
        .RequireAuthorization();
    }
}
