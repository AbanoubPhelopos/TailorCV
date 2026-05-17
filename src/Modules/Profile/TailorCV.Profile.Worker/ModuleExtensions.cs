using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.Infrastructure.Storage;
using TailorCV.Profile.Worker.Infrastructure.AI;
using TailorCV.Infrastructure.AI;

namespace TailorCV.Profile.Worker;

public static class ModuleExtensions
{
    public static IServiceCollection AddProfileWorkerServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        IConfiguration module = config.GetSection("Profile");

        services.AddOpenAIClient(module, "OpenAI");
        services.AddScoped<IResumeParserService, OpenAiResumeParserService>();
        services.AddBlobStorage(config);

        return services;
    }
}
