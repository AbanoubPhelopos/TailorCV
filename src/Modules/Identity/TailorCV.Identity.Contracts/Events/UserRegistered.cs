namespace TailorCV.Identity.Contracts.Events;

public record UserRegistered(Guid UserId, string FirstName, string LastName);
