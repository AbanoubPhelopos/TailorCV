using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace TailorCV.Infrastructure.AI;

public static class OpenAiServiceExtensions
{
    public static IServiceCollection AddOpenAIClient(
        this IServiceCollection services,
        IConfiguration section)
    {
        services.AddOptions<OpenAiOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<OpenAIClient>((sp) =>
        {
            IOptions<OpenAiOptions> options = sp.GetRequiredService<IOptions<OpenAiOptions>>();
            return string.IsNullOrEmpty(options.Value.Endpoint)
                ? new OpenAIClient(options.Value.ApiKey)
                : new OpenAIClient(
                    new ApiKeyCredential(options.Value.ApiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(options.Value.Endpoint) });
        });

        return services;
    }

    public static IServiceCollection AddOpenAIClient(
        this IServiceCollection services,
        IConfiguration parentSection,
        string relativePath)
    {
        return services.AddOpenAIClient(parentSection.GetSection(relativePath));
    }
}
