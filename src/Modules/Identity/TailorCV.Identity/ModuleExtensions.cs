using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.Identity.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.EntityFramework;

namespace TailorCV.Identity;

public static class ModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        IConfiguration module = config.GetSection("Identity");

        services.AddModuleDbContext<IdentityDbContext>(config, "identity");
        services.AddCQRSHandlers(typeof(IdentityDbContext).Assembly);

        services.AddOptions<JwtSettings>()
            .Bind(module.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IJwtService, JwtService>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        Features.Register.MapEndpoint(app);
        Features.Login.MapEndpoint(app);
        Features.RefreshToken.MapEndpoint(app);
        Features.Logout.MapEndpoint(app);
        Features.UpdateUserName.MapEndpoint(app);
        return app;
    }
}
