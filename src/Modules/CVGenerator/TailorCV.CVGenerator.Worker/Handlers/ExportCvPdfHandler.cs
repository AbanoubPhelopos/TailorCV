using Grpc.Core;
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Worker.Infrastructure.Pdf;
using TailorCV.Shared.Interfaces;
using TailorCV.Templates.Contracts.Grpc;

#pragma warning disable S108

namespace TailorCV.CVGenerator.Worker.Handlers;

public static class ExportCvPdfHandler
{
    public static async Task<object> HandleAsync(
        Contracts.Commands.ExportCvPdf command,
        TemplatesService.TemplatesServiceClient templateClient,
        IPdfRenderer pdfRenderer,
        IBlobStorage blobStorage,
        CancellationToken ct)
    {
        try
        {
            GetTemplateByIdResponse template;
            try
            {
                template = await templateClient.GetTemplateByIdAsync(
                    new GetTemplateByIdRequest { Id = command.TemplateId.ToString() },
                    cancellationToken: ct);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return new CvPdfExportFailed(command.GenerationId, "Template not found");
            }

            string fullHtml = BuildFullHtml(template.HtmlContent, template.CssContent, command.Content);

            byte[] pdfBytes = await pdfRenderer.RenderAsync(fullHtml, template.CssContent, ct);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            string pdfKey = $"cvs/{now:yyyy}/{now:MM}/{now:dd}/{Guid.CreateVersion7()}.pdf";

            await using Stream stream = new MemoryStream(pdfBytes);
            await blobStorage.UploadAsync(stream, pdfKey, "application/pdf", ct);

            return new CvPdfExportCompleted(command.GenerationId, pdfKey);
        }
        catch (Exception ex)
        {
            return new CvPdfExportFailed(command.GenerationId, ex.Message);
        }
    }

    private static string BuildFullHtml(string html, string css, string contentJson)
    {
        string styledHtml = html.Replace("</head>", $"<style>{css}</style></head>");

        try
        {
            using JsonDocument doc = JsonDocument.Parse(contentJson);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("summary", out JsonElement summary))
            {
                styledHtml = styledHtml.Replace("{{summary}}", summary.GetString() ?? string.Empty);
            }

            if (root.TryGetProperty("sections", out JsonElement sections))
            {
                styledHtml = styledHtml.Replace("{{sections}}", sections.GetRawText());
            }
        }
        catch (JsonException)
        {
        }

        return styledHtml;
    }
}
