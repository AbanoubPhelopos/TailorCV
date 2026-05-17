using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.EntityFramework;

namespace TailorCV.JobDescriptions;

public static class ModuleExtensions
{
    public static IServiceCollection AddJobDescriptionsModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddModuleDbContext<JobDescriptionsDbContext>(config, "jobdescriptions");
        services.AddCQRSHandlers(typeof(JobDescriptionsDbContext).Assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapJobDescriptionsEndpoints(this IEndpointRouteBuilder app)
    {
        Features.ParseJobDescription.MapEndpoint(app);
        Features.ScrapeJobDescription.MapEndpoint(app);
        Features.GetParseStatus.MapEndpoint(app);
        Features.SaveJobDescription.MapEndpoint(app);
        Features.ListJobs.MapEndpoint(app);
        Features.GetJob.MapEndpoint(app);
        return app;
    }
}
