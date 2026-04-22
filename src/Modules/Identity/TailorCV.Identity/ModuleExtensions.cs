using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TailorCV.Identity.Infrastructure;
using TailorCV.Shared.CQRS;

namespace TailorCV.Identity;

public static class ModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .UseSnakeCaseNamingConvention());

        services.Scan(scan => scan
            .FromAssemblyOf<IdentityDbContext>()
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<IdentityDbContext>()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandLoggingDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryLoggingDecorator<,>));

        services.AddOptions<JwtSettings>()
            .Bind(config.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IJwtService, JwtService>();

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

    public static async Task MigrateIdentityModuleAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        Features.Register.MapEndpoint(app);
        Features.Login.MapEndpoint(app);
        Features.RefreshToken.MapEndpoint(app);
        Features.Logout.MapEndpoint(app);
        return app;
    }
}
