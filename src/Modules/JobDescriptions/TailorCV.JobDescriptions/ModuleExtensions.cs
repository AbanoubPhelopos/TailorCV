using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.CQRS;

namespace TailorCV.JobDescriptions;

public static class ModuleExtensions
{
    public static IServiceCollection AddJobDescriptionsModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<JobDescriptionsDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "jobdescriptions"))
            .UseSnakeCaseNamingConvention());

        services.Scan(scan => scan
            .FromAssemblyOf<JobDescriptionsDbContext>()
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<JobDescriptionsDbContext>()
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

    public static async Task MigrateJobDescriptionsModuleAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        JobDescriptionsDbContext dbContext = scope.ServiceProvider.GetRequiredService<JobDescriptionsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static IEndpointRouteBuilder MapJobDescriptionsEndpoints(this IEndpointRouteBuilder app)
    {
        Features.ParseJobDescription.MapEndpoint(app);
        Features.ScrapeJobDescription.MapEndpoint(app);
        Features.GetParseStatus.MapEndpoint(app);
        Features.SaveJobDescription.MapEndpoint(app);
        Features.ListJobs.MapEndpoint(app);
        Features.GetJob.MapEndpoint(app);
        return app;
    }
}
