using Microsoft.EntityFrameworkCore;
using TailorCV.Identity.Contracts.Events;
using TailorCV.Profile.Infrastructure;

namespace TailorCV.Profile.Events;

public static class UserNameUpdatedHandler
{
    public static async Task HandleAsync(
        UserNameUpdated @event,
        ProfileDbContext dbContext,
        CancellationToken ct)
    {
        Domain.ProfileUser? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == @event.UserId, ct);

        if (user is null)
        {
            return;
        }

        user.UpdateName(@event.FirstName, @event.LastName);
        await dbContext.SaveChangesAsync(ct);
    }
}
