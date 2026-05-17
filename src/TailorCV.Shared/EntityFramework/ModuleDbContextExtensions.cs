using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TailorCV.Shared.EntityFramework;

public static class ModuleDbContextExtensions
{
    public static IServiceCollection AddModuleDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration config,
        string schema)
        where TDbContext : DbContext
    {
        services.AddDbContext<TDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .UseSnakeCaseNamingConvention());

        return services;
    }

    public static async Task MigrateModuleAsync<TDbContext>(
        this WebApplication app)
        where TDbContext : DbContext
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using IServiceScope scope = app.Services.CreateScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
