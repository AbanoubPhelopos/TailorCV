using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using TailorCV.Infrastructure.Storage;
using TailorCV.Profile.Worker.Infrastructure.AI;

namespace TailorCV.Profile.Worker;

public static class ModuleExtensions
{
    public static IServiceCollection AddProfileWorkerServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddOptions<OpenAiOptions>()
            .Bind(config.GetSection(OpenAiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<OpenAIClient>(sp =>
        {
            IOptions<OpenAiOptions> options = sp.GetRequiredService<IOptions<OpenAiOptions>>();
            return string.IsNullOrEmpty(options.Value.Endpoint)
                ? new OpenAIClient(options.Value.ApiKey)
                : new OpenAIClient(new ApiKeyCredential(options.Value.ApiKey), new OpenAIClientOptions { Endpoint = new Uri(options.Value.Endpoint) });
        });

        services.AddScoped<IResumeParserService, OpenAiResumeParserService>();

        services.AddBlobStorage(config);

        return services;
    }
}
