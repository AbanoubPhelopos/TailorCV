using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.JobDescriptions.Contracts.Grpc;
using TailorCV.Profile.Contracts.Grpc;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.EntityFramework;
using TailorCV.Templates.Contracts.Grpc;

namespace TailorCV.CVGenerator;

public static class ModuleExtensions
{
    public static IServiceCollection AddCVGeneratorModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        IConfiguration module = config.GetSection("CVGenerator");

        services.AddModuleDbContext<CVGeneratorDbContext>(config, "cvgenerator");
        services.AddCQRSHandlers(typeof(AssemblyMarker).Assembly);

        string internalGrpcAddress = module["InternalGrpc:Address"] ?? "http://localhost:8080";
        services.AddSingleton(_ =>
        {
            GrpcChannel channel = GrpcChannel.ForAddress(internalGrpcAddress);
            return new ProfileService.ProfileServiceClient(channel);
        });
        services.AddSingleton(_ =>
        {
            GrpcChannel channel = GrpcChannel.ForAddress(internalGrpcAddress);
            return new JobDescriptionsService.JobDescriptionsServiceClient(channel);
        });
        services.AddSingleton(_ =>
        {
            GrpcChannel channel = GrpcChannel.ForAddress(internalGrpcAddress);
            return new TemplatesService.TemplatesServiceClient(channel);
        });

        return services;
    }

    public static IEndpointRouteBuilder MapCVGeneratorEndpoints(this IEndpointRouteBuilder app)
    {
        Features.GenerateCV.MapEndpoint(app);
        Features.GetGenerationStatus.MapEndpoint(app);
        Features.GetGeneratedCV.MapEndpoint(app);
        Features.ListHistory.MapEndpoint(app);
        Features.UpdateCVContent.MapEndpoint(app);
        Features.RegenerateCV.MapEndpoint(app);
        Features.GenerateCoverLetter.MapEndpoint(app);
        Features.ExportPdf.MapEndpoint(app);
        return app;
    }
}
