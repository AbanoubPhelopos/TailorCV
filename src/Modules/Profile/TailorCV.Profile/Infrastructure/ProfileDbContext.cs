using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;

namespace TailorCV.Profile.Infrastructure;

public class ProfileDbContext : DbContext
{
    public DbSet<Domain.Profile> Profiles => Set<Domain.Profile>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Education> Education => Set<Education>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<CustomSection> CustomSections => Set<CustomSection>();
    public DbSet<SectionOrder> SectionOrders => Set<SectionOrder>();
    public DbSet<ParseJob> ParseJobs => Set<ParseJob>();

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("profile");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);
    }
}
