using TailorCV.Modules.Identity.Abstractions.Authentication;
using TailorCV.Modules.Identity.Abstractions.Data;
using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using TailorCV.SharedKernel;

namespace TailorCV.Modules.Identity.Users.Login;

internal sealed class LoginUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider) : ICommandHandler<LoginUserCommand, string>
{
    public async Task<Result<string>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure<string>(UserErrors.NotFoundByEmail);
        }

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!verified)
        {
            return Result.Failure<string>(UserErrors.NotFoundByEmail);
        }

        string token = tokenProvider.Create(user);

        return token;
    }
}
