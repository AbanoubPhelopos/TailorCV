using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using TailorCV.Api.Middleware;
using TailorCV.Api.OpenApi;
using TailorCV.Api.Services;
using TailorCV.Identity;
using TailorCV.Identity.Infrastructure;
using TailorCV.JobScraper;
using TailorCV.Shared.Interfaces;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddHealthChecks();

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = (info) =>
    {
        if (info.Type.DeclaringType is { } declaringType)
        {
            return $"{declaringType.Name}{info.Type.Name}";
        }

        return null;
    };
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddJobScraperModule(builder.Configuration);

JwtSettings jwtSettings = new();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

await app.MigrateIdentityModuleAsync();
await app.MigrateJobScraperModuleAsync();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("TailorCV API")
            .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme);
    });
}

app.MapHealthChecks("/health");
app.MapIdentityEndpoints();
app.MapJobScraperEndpoints();

app.MapGet("/", () => "TailorCV API");

await app.RunAsync();
