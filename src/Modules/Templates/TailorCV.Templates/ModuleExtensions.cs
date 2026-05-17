using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TailorCV.Templates.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.EntityFramework;
using TailorCV.Templates.Infrastructure.Seeding;

namespace TailorCV.Templates;

public static class ModuleExtensions
{
    public static IServiceCollection AddTemplatesModule(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddModuleDbContext<TemplatesDbContext>(config, "templates");
        services.AddCQRSHandlers(typeof(TemplatesDbContext).Assembly);

        return services;
    }

    public static async Task MigrateTemplatesModuleAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        TemplatesDbContext dbContext = scope.ServiceProvider.GetRequiredService<TemplatesDbContext>();
        await dbContext.Database.MigrateAsync();
        await TemplateSeeder.SeedAsync(dbContext);
    }

    public static IEndpointRouteBuilder MapTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        Features.BrowseTemplates.MapEndpoint(app);
        Features.GetTemplate.MapEndpoint(app);
        Features.PreviewTemplate.MapEndpoint(app);
        Features.UploadTemplateThumbnail.MapEndpoint(app);
        Features.CreateTemplate.MapEndpoint(app);
        Features.UpdateTemplate.MapEndpoint(app);
        Features.DisableTemplate.MapEndpoint(app);
        return app;
    }
}
