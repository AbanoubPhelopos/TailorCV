namespace TailorCV.Profile.Contracts.Events;

public record ProfileUpdatedEvent(Guid UserId, Guid ProfileId, DateTimeOffset UpdatedAt);
