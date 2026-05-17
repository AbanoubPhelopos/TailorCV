using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.CVGenerator.Worker.Infrastructure.AI;
using TailorCV.CVGenerator.Worker.Infrastructure.Pdf;
using TailorCV.CVGenerator.Worker.Infrastructure.Scoring;
using TailorCV.Infrastructure.Storage;
using TailorCV.Infrastructure.AI;
using TailorCV.Templates.Contracts.Grpc;

namespace TailorCV.CVGenerator.Worker;

public static class ModuleExtensions
{
    public static IServiceCollection AddCVGeneratorWorkerServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        IConfiguration module = config.GetSection("CVGenerator");

        services.AddOpenAIClient(module, "OpenAI");

        string internalGrpcAddress = module["InternalGrpc:Address"] ?? "http://localhost:8080";
        services.AddSingleton(_ =>
        {
            GrpcChannel channel = GrpcChannel.ForAddress(internalGrpcAddress);
            return new TemplatesService.TemplatesServiceClient(channel);
        });

        services.AddSingleton<IMatchScoreCalculator, MatchScoreCalculator>();
        services.AddSingleton<IPdfRenderer, PuppeteerPdfRenderer>();

        services.AddScoped<ICVTailoringService, OpenAiCVTailoringService>();
        services.AddScoped<ICoverLetterService, OpenAiCoverLetterService>();

        services.AddBlobStorage(config);

        return services;
    }
}
