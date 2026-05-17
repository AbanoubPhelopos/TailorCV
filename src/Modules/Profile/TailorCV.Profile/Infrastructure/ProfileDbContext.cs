using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure;

public class ProfileDbContext : DbContext
{
    public DbSet<Domain.Profile> Profiles => Set<Domain.Profile>();
    public DbSet<ParseJob> ParseJobs => Set<ParseJob>();
    public DbSet<Domain.ProfileUser> Users => Set<Domain.ProfileUser>();

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("profile");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);
    }
}
