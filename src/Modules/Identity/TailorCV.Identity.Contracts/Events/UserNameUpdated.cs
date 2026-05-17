namespace TailorCV.Identity.Contracts.Events;

public record UserNameUpdated(Guid UserId, string FirstName, string LastName);
