using TailorCV.Modules.Identity.Domain.Users;

namespace TailorCV.Modules.Identity.Abstractions.Authentication;

public interface ITokenProvider
{
    string Create(User user);
}
