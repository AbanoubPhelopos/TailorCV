using System.Reflection;
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
using TailorCV.Identity;
using TailorCV.Identity.Infrastructure;
using TailorCV.JobDescriptions;
using TailorCV.Profile;
using TailorCV.Profile.Infrastructure;
using TailorCV.Infrastructure.Storage;
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

    opts.PublishMessage<TailorCV.JobDescriptions.Contracts.Commands.ScrapeJobUrl>()
        .ToRabbitQueue("job-description.commands");
    opts.PublishMessage<TailorCV.JobDescriptions.Contracts.Commands.ParseJobText>()
        .ToRabbitQueue("job-description.commands");

    opts.ListenToRabbitQueue("job-description.events");

    opts.PublishMessage<TailorCV.Profile.Contracts.Commands.ParseResume>()
        .ToRabbitQueue("profile.commands");

    opts.PublishMessage<TailorCV.Profile.Contracts.Events.ProfileUpdated>()
        .ToRabbitQueue("profile.events");

    opts.ListenToRabbitQueue("profile.events");

    opts.ApplicationAssembly = typeof(TailorCV.Api.ModuleMarker).Assembly;
    opts.ServiceName = "TailorCV.Api";
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
builder.Services.AddJobDescriptionsModule(builder.Configuration);
builder.Services.AddProfileModule(builder.Configuration);
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
            ValidIssuer = builder.Configuration[$"{JwtSettings.SectionName}:Issuer"],
            ValidAudience = builder.Configuration[$"{JwtSettings.SectionName}:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[$"{JwtSettings.SectionName}:Secret"]!)),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

await app.MigrateIdentityModuleAsync();
await app.MigrateJobDescriptionsModuleAsync();
await app.MigrateProfileModuleAsync();

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

app.MapGet("/", () => "TailorCV API");

await app.RunAsync();
