using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Serilog;
using TailorCV.JobDescriptions.Worker;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

if (builder.Environment.IsDevelopment())
{
    try
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
    }
    catch (Exception ex)
    {
        await Console.Error.WriteLineAsync(ex.Message);
        Environment.Exit(1);
    }
}

builder.Services.AddWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration["RabbitMQ:ConnectionString"]!)
        .AutoProvision();

    opts.ListenToRabbitQueue("job-description.commands")
        .MaximumParallelMessages(3);

    opts.PublishMessage<TailorCV.JobDescriptions.Contracts.Events.JobParsingCompleted>()
        .ToRabbitQueue("job-description.events");
    opts.PublishMessage<TailorCV.JobDescriptions.Contracts.Events.JobParsingFailed>()
        .ToRabbitQueue("job-description.events");

    opts.ApplicationAssembly = typeof(ModuleExtensions).Assembly;
    opts.ServiceName = "TailorCV.JobDescriptions.Worker";
});

builder.Services.AddJobDescriptionWorkerServices(builder.Configuration);

WebApplication app = builder.Build();

await app.RunAsync();
