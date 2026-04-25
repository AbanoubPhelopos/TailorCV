using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Infrastructure.Storage;

public class BlobStorageInitializer : IHostedService
{
    private readonly IBlobStorage _blobStorage;
    private readonly ILogger<BlobStorageInitializer> _logger;

    public BlobStorageInitializer(IBlobStorage blobStorage, ILogger<BlobStorageInitializer> logger)
    {
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing blob storage...");
        await _blobStorage.InitializeAsync(cancellationToken);
        _logger.LogInformation("Blob storage initialized");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
