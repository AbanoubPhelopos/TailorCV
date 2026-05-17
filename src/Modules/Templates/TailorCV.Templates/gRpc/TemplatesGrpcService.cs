using Grpc.Core;
using TailorCV.Templates.Contracts.Grpc;
using TailorCV.Templates.Infrastructure;

namespace TailorCV.Templates.gRpc;

public class TemplatesGrpcService(
    TemplatesDbContext dbContext) : TemplatesService.TemplatesServiceBase
{
    public override async Task<GetTemplateByIdResponse> GetTemplateById(
        GetTemplateByIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out Guid templateId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid template ID format"));
        }

        Domain.Template? template = await dbContext.Templates
            .FindAsync([templateId], context.CancellationToken);

        if (template is null || !template.IsActive)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Template not found"));
        }

        return new GetTemplateByIdResponse
        {
            Id = template.Id.ToString(),
            Name = template.Name,
            HtmlContent = template.HtmlContent,
            CssContent = template.CssContent,
            ThumbnailUrl = template.ThumbnailUrl,
            Category = template.Category,
            Style = template.Style,
            IsActive = template.IsActive,
        };
    }
}
