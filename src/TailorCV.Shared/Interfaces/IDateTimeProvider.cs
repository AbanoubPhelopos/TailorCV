namespace TailorCV.Shared.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
