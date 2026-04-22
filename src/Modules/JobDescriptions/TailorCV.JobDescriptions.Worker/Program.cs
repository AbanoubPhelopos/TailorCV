using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TailorCV.JobDescriptions.Contracts.Events;
using TailorCV.JobDescriptions.Worker;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

if (builder.Environment.IsDevelopment() && !IsPlaywrightInstalled(builder.Configuration))
{
    Microsoft.Playwright.Program.Main(["install", "chromium"]);
}

static bool IsPlaywrightInstalled(IConfiguration config) =>
    config["PLAYWRIGHT_SKIP_INSTALL"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

builder.Services.AddWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration["RabbitMQ:ConnectionString"]!).AutoProvision();
    opts.ListenToRabbitQueue("job-description.commands").MaximumParallelMessages(3);
    opts.PublishMessage<JobParsingCompleted>().ToRabbitQueue("job-description.events");
    opts.PublishMessage<JobParsingFailed>().ToRabbitQueue("job-description.events");
    opts.ApplicationAssembly = typeof(ModuleExtensions).Assembly;
    opts.ServiceName = "TailorCV.JobDescriptions.Worker";
});

builder.Services.AddJobDescriptionWorkerServices(builder.Configuration);

WebApplication app = builder.Build();
await app.RunAsync();
