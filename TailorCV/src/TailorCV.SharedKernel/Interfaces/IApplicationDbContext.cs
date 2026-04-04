using TailorCV.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TailorCV.Modules.Identity.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
