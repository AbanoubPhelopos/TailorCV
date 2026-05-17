using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using TailorCV.Api.Middleware;
using TailorCV.Api.OpenApi;
using TailorCV.Api.Services;
using TailorCV.CVGenerator;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Identity;
using TailorCV.Identity.Infrastructure;
using TailorCV.JobDescriptions;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.JobDescriptions.gRpc;
using TailorCV.Profile;
using TailorCV.Profile.Infrastructure;
using TailorCV.Profile.gRpc;
using TailorCV.Templates;
using TailorCV.Templates.gRpc;
using TailorCV.Infrastructure.Storage;
using TailorCV.Shared.EntityFramework;
using TailorCV.Shared.Interfaces;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration["RabbitMQ:ConnectionString"]!)
        .AutoProvision();

    opts.ApplicationAssembly = typeof(TailorCV.Api.ModuleMarker).Assembly;
    opts.ServiceName = "TailorCV.Api";
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql")
    .AddRabbitMQ(
        async _ =>
        {
            System.Uri uri = new(builder.Configuration["RabbitMQ:ConnectionString"]!);
            RabbitMQ.Client.ConnectionFactory factory = new() { Uri = uri };
            return await factory.CreateConnectionAsync();
        },
        name: "rabbitmq")
    .AddRedis(builder.Configuration["Redis:Configuration"]!, name: "redis");

builder.Services.AddGrpc();

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = (info) =>
    {
        if (info.Type.DeclaringType is { } declaringType)
        {
            return $"{declaringType.Name}{info.Type.Name}";
        }

        return info.Type.Name;
    };
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddJobDescriptionsModule(builder.Configuration);
builder.Services.AddProfileModule(builder.Configuration);
builder.Services.AddTemplatesModule(builder.Configuration);
builder.Services.AddCVGeneratorModule(builder.Configuration);
builder.Services.AddBlobStorage(builder.Configuration);

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
            ValidIssuer = builder.Configuration["Identity:Jwt:Issuer"],
            ValidAudience = builder.Configuration["Identity:Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Identity:Jwt:Secret"]!)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

WebApplication app = builder.Build();

await app.MigrateModuleAsync<IdentityDbContext>();
await app.MigrateModuleAsync<JobDescriptionsDbContext>();
await app.MigrateModuleAsync<ProfileDbContext>();
await app.MigrateTemplatesModuleAsync();
await app.MigrateModuleAsync<CVGeneratorDbContext>();

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
app.MapJobDescriptionsEndpoints();
app.MapProfileEndpoints();
app.MapTemplatesEndpoints();
app.MapCVGeneratorEndpoints();

app.MapGrpcService<TemplatesGrpcService>();
app.MapGrpcService<ProfileGrpcService>();
app.MapGrpcService<JobDescriptionsGrpcService>();

app.MapGet("/", () => "TailorCV API");

await app.RunAsync();
