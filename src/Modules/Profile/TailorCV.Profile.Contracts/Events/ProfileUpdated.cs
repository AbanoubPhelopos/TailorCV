namespace TailorCV.Profile.Contracts.Events;

public record ProfileUpdated(Guid UserId, Guid ProfileId, DateTimeOffset UpdatedAt);
