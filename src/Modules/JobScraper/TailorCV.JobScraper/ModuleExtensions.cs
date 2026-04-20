using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TailorCV.JobScraper.Infrastructure;
using TailorCV.Shared.CQRS;

namespace TailorCV.JobScraper;

public static class ModuleExtensions
{
    public static IServiceCollection AddJobScraperModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<JobScraperDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "jobscraper"))
            .UseSnakeCaseNamingConvention());

        services.Scan(scan => scan
            .FromAssemblyOf<JobScraperDbContext>()
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<JobScraperDbContext>()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandLoggingDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryLoggingDecorator<,>));

        return services;
    }

    private static void TryDecorate(
        IServiceCollection services,
        Type serviceType,
        Type decoratorType)
    {
        bool hasRegistration = services.Any(s => s.ServiceType.IsGenericType
            && s.ServiceType.GetGenericTypeDefinition() == serviceType);

        if (hasRegistration)
        {
            services.Decorate(serviceType, decoratorType);
        }
    }

    public static async Task MigrateJobScraperModuleAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        JobScraperDbContext dbContext = scope.ServiceProvider.GetRequiredService<JobScraperDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static IEndpointRouteBuilder MapJobScraperEndpoints(this IEndpointRouteBuilder app)
    {
        Features.ParseJobDescription.MapEndpoint(app);
        Features.ScrapeJobUrl.MapEndpoint(app);
        Features.GetParseStatus.MapEndpoint(app);
        Features.SaveJobDescription.MapEndpoint(app);
        Features.ListJobs.MapEndpoint(app);
        Features.GetJob.MapEndpoint(app);
        return app;
    }
}
