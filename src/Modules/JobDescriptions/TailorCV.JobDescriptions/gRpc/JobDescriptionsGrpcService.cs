using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TailorCV.JobDescriptions.Contracts.Grpc;
using TailorCV.JobDescriptions.Infrastructure;

namespace TailorCV.JobDescriptions.gRpc;

public class JobDescriptionsGrpcService(
    JobDescriptionsDbContext dbContext) : JobDescriptionsService.JobDescriptionsServiceBase
{
    public override async Task<GetJobByIdResponse> GetJobById(
        GetJobByIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out Guid jobId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid job ID format"));
        }

        Domain.JobDescription? job = await dbContext.JobDescriptions
            .FirstOrDefaultAsync(j => j.Id == jobId, context.CancellationToken);

#pragma warning disable IDE0270
        if (job is null)
#pragma warning restore IDE0270
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Job description not found"));
        }

        GetJobByIdResponse response = new()
        {
            Id = job.Id.ToString(),
            Title = job.Title,
            Company = job.Company,
            Location = job.Location ?? string.Empty,
            SeniorityLevel = job.SeniorityLevel?.ToString() ?? string.Empty,
            UserId = job.UserId.ToString(),
        };

        response.RequiredSkills.AddRange(job.RequiredSkills);
        response.Responsibilities.AddRange(job.Responsibilities);
        response.Qualifications.AddRange(job.Qualifications);

        return response;
    }
}
