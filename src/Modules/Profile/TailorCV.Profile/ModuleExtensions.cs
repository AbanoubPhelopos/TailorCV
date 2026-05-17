using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.EntityFramework;

namespace TailorCV.Profile;

public static class ModuleExtensions
{
    public static IServiceCollection AddProfileModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddModuleDbContext<ProfileDbContext>(config, "profile");
        services.AddCQRSHandlers(typeof(ProfileDbContext).Assembly);

        return services;
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
