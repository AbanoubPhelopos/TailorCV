using TailorCV.Shared.Interfaces;

namespace TailorCV.Api.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => TimeProvider.System.GetUtcNow();
}
