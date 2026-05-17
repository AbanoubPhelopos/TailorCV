using Microsoft.EntityFrameworkCore;
using TailorCV.Identity.Contracts.Events;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;

namespace TailorCV.Profile.Events;

public static class UserRegisteredHandler
{
    public static async Task HandleAsync(
        UserRegistered @event,
        ProfileDbContext dbContext,
        CancellationToken ct)
    {
        bool exists = await dbContext.Users
            .AnyAsync(u => u.UserId == @event.UserId, ct);

        if (exists)
        {
            return;
        }

        ProfileUser user = ProfileUser.Create(
            @event.UserId,
            @event.FirstName,
            @event.LastName);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);
    }
}
