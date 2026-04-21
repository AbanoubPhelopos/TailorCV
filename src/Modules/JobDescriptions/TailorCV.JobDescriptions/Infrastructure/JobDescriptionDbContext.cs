using Microsoft.EntityFrameworkCore;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Infrastructure.Configurations;

namespace TailorCV.JobDescriptions.Infrastructure;

public class JobDescriptionsDbContext : DbContext
{
    public DbSet<ParseJob> ParseJobs => Set<ParseJob>();
    public DbSet<JobDescription> JobDescriptions => Set<JobDescription>();

    public JobDescriptionsDbContext(DbContextOptions<JobDescriptionsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("jobdescriptions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobDescriptionsDbContext).Assembly);
    }
}
