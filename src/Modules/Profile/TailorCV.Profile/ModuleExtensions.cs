using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;

namespace TailorCV.Profile;

public static class ModuleExtensions
{
    public static IServiceCollection AddProfileModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<ProfileDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "profile"))
            .UseSnakeCaseNamingConvention());

        services.Scan(scan => scan
            .FromAssemblyOf<ProfileDbContext>()
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<ProfileDbContext>()
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

    public static async Task MigrateProfileModuleAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        ProfileDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        Features.CreateProfile.MapEndpoint(app);
        Features.GetProfile.MapEndpoint(app);
        Features.UpdateProfile.MapEndpoint(app);
        Features.UpdateSections.MapEndpoint(app);
        Features.GetCompleteness.MapEndpoint(app);
        Features.ExportProfile.MapEndpoint(app);
        Features.ShareProfile.MapEndpoint(app);
        Features.GetSharedProfile.MapEndpoint(app);
        Features.ImportResumeGetUploadUrl.MapEndpoint(app);
        Features.ImportResumeParse.MapEndpoint(app);
        Features.ImportResumeParseStatus.MapEndpoint(app);
        Features.ImportResumeConfirm.MapEndpoint(app);
        return app;
    }
}
