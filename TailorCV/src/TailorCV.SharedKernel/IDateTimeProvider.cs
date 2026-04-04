namespace TailorCV.SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
