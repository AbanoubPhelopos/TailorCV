using TailorCV.SharedKernel;

namespace TailorCV.Modules.Identity.Domain.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;
