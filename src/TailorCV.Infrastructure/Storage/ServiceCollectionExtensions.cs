using Amazon;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Infrastructure.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BlobStorageOptions>(
            configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            IOptions<BlobStorageOptions> options = sp.GetRequiredService<IOptions<BlobStorageOptions>>();
            BlobStorageOptions opts = options.Value;

            AmazonS3Config s3Config = new()
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = opts.Endpoint,
                ForcePathStyle = opts.ForcePathStyle,
            };

            AmazonS3Client client = new(opts.AccessKey, opts.SecretKey, s3Config);
            return client;
        });

        services.AddSingleton<IBlobStorage, S3BlobStorage>();
        services.AddSingleton<IHostedService, BlobStorageInitializer>();

        return services;
    }
}
