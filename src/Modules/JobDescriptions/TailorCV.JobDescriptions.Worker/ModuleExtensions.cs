using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using TailorCV.JobDescriptions.Worker.Infrastructure.AI;
using TailorCV.JobDescriptions.Worker.Infrastructure.RateLimiting;
using TailorCV.JobDescriptions.Worker.Infrastructure.Scraping;

namespace TailorCV.JobDescriptions.Worker;

public static class ModuleExtensions
{
    public static IServiceCollection AddJobDescriptionWorkerServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<OpenAiOptions>(config.GetSection("OpenAI"));
        services.Configure<PlaywrightOptions>(config.GetSection("Playwright"));

        services.AddSingleton<DomainRateLimiter>();
        services.AddSingleton<IPlaywrightScrapingService, PlaywrightScrapingService>();

        services.AddSingleton<OpenAIClient>((sp) =>
        {
            IOptions<OpenAiOptions> options = sp.GetRequiredService<IOptions<OpenAiOptions>>();
            return string.IsNullOrEmpty(options.Value.Endpoint)
                ? new OpenAIClient(options.Value.ApiKey)
                : new OpenAIClient(new ApiKeyCredential(options.Value.ApiKey), new OpenAIClientOptions { Endpoint = new Uri(options.Value.Endpoint) });
        });

        services.AddScoped<IJobDescriptionParserService, OpenAiJobParserService>();

        return services;
    }
}