using Microsoft.EntityFrameworkCore;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Infrastructure.Configurations;

namespace TailorCV.JobScraper.Infrastructure;

public class JobScraperDbContext : DbContext
{
    public DbSet<ParseJob> ParseJobs => Set<ParseJob>();
    public DbSet<JobDescription> JobDescriptions => Set<JobDescription>();

    public JobScraperDbContext(DbContextOptions<JobScraperDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("jobscraper");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobScraperDbContext).Assembly);
    }
}
