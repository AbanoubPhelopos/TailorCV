using TailorCV.Modules.Identity.Abstractions.Messaging;

namespace TailorCV.Modules.Identity.Users.GetById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
