#pragma warning disable CA1054
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Commands;
using TailorCV.CVGenerator.Domain;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using TailorCV.Templates.Contracts.Grpc;
using Wolverine;

namespace TailorCV.CVGenerator.Features;

public static class ExportPdf
{
    public record TriggerRequest(Guid Id);

    public record TriggerResponse(Guid ExportId);

    public record StatusResponse(string Status, string? DownloadUrl = null, string? Error = null);

    public class TriggerHandler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<TriggerRequest, TriggerResponse>
    {
        public async Task<Result<TriggerResponse>> HandleAsync(TriggerRequest command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<TriggerResponse>.Failure(CVErrors.CVNotFound);
            }

            if (cv.Status != GenerationStatus.Done || cv.Content is null)
            {
                return Result<TriggerResponse>.Failure(CVErrors.CVContentNotReady);
            }

            if (cv.PdfStatus == PdfStatus.Ready && cv.PdfKey is not null)
            {
                return Result<TriggerResponse>.Success(new TriggerResponse(cv.Id));
            }

            DateTimeOffset now = dateTimeProvider.UtcNow;
            cv.StartPdfExport(now);
            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new ExportCvPdf(
                cv.Id,
                cv.TemplateId,
                cv.Content,
                cv.ProfileSnapshot,
                cv.JobSnapshot));

            return Result<TriggerResponse>.Success(new TriggerResponse(cv.Id));
        }
    }

    public class StatusHandler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<TriggerRequest, StatusResponse>
    {
        public async Task<Result<StatusResponse>> HandleAsync(TriggerRequest query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == query.Id && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<StatusResponse>.Failure(CVErrors.CVNotFound);
            }

            return cv.PdfStatus switch
            {
                PdfStatus.Pending => Result<StatusResponse>.Success(new StatusResponse("PROCESSING")),
                PdfStatus.Ready => Result<StatusResponse>.Success(
                    new StatusResponse("DONE", $"/api/cv/{cv.Id}/export/pdf")),
                PdfStatus.Failed => Result<StatusResponse>.Success(
                    new StatusResponse("FAILED", Error: "Failed to generate PDF")),
                _ => Result<StatusResponse>.Success(new StatusResponse("PROCESSING"))
            };
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cv/{id:guid}/export/pdf", async (
            Guid id,
            ICommandHandler<TriggerRequest, TriggerResponse> handler,
            CancellationToken ct) =>
        {
            Result<TriggerResponse> result = await handler.HandleAsync(new TriggerRequest(id), ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/cv/{id}/export/status", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("ExportPdf")
        .WithSummary("Export CV as PDF")
        .WithDescription("Triggers PDF generation. If already cached, returns 202 immediately. Poll status for download URL.")
        .Produces<TriggerResponse>(202)
        .RequireAuthorization();

        app.MapGet("/api/cv/{id:guid}/export/status", async (
            Guid id,
            IQueryHandler<TriggerRequest, StatusResponse> handler,
            CancellationToken ct) =>
        {
            Result<StatusResponse> result = await handler.HandleAsync(new TriggerRequest(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("GetExportStatus")
        .WithSummary("Poll PDF export status")
        .WithDescription("Returns the current status of PDF export. When DONE, use download endpoint.")
        .Produces<StatusResponse>()
        .RequireAuthorization();

        app.MapGet("/api/cv/{id:guid}/export/pdf", async (
            Guid id,
            CVGeneratorDbContext dbContext,
            IBlobStorage blobStorage,
            ICurrentUserService currentUserService,
            CancellationToken ct) =>
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

            if (cv is null || cv.PdfStatus != PdfStatus.Ready || cv.PdfKey is null)
            {
                return Results.NotFound(new { code = "PDF_NOT_READY", message = "PDF not ready yet" });
            }

            Stream? pdfStream = await blobStorage.DownloadAsync(cv.PdfKey, ct);

            if (pdfStream is null)
            {
                return Results.NotFound(new { code = "PDF_NOT_READY", message = "PDF file not found" });
            }

            string fileName = $"cv_{id:N}.pdf";

            return Results.Stream(pdfStream, "application/pdf", fileName);
        })
        .WithTags("CVGenerator")
        .WithName("DownloadPdf")
        .WithSummary("Download generated PDF")
        .WithDescription("Downloads the generated PDF file. Only available when PDF status is Ready.")
        .Produces<FileStream>(200, "application/pdf")
        .RequireAuthorization();
    }
}
