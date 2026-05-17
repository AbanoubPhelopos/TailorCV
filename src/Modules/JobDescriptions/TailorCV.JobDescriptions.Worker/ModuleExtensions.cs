using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.JobDescriptions.Worker.Infrastructure.AI;
using TailorCV.JobDescriptions.Worker.Infrastructure.RateLimiting;
using TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;
using TailorCV.Infrastructure.AI;

namespace TailorCV.JobDescriptions.Worker;

public static class ModuleExtensions
{
    public static IServiceCollection AddJobDescriptionWorkerServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        IConfiguration module = config.GetSection("JobDescriptions");

        services.AddOpenAIClient(module, "OpenAI");

        services.AddOptions<PlaywrightOptions>()
            .Bind(module.GetSection(PlaywrightOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<DomainRateLimiter>();
        services.AddSingleton<IPlaywrightScrapingService, PlaywrightScrapingService>();
        services.AddScoped<IJobDescriptionParserService, OpenAiJobParserService>();

        return services;
    }
}
