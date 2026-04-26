using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TailorCV.Profile.Contracts.Events;
using TailorCV.Profile.Worker;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration["RabbitMQ:ConnectionString"]!).AutoProvision();
    opts.ListenToRabbitQueue("profile.commands").MaximumParallelMessages(3);
    opts.PublishMessage<ResumeParsingCompleted>().ToRabbitQueue("profile.events");
    opts.PublishMessage<ResumeParsingFailed>().ToRabbitQueue("profile.events");
    opts.ApplicationAssembly = typeof(ModuleExtensions).Assembly;
    opts.ServiceName = "TailorCV.Profile.Worker";
});

builder.Services.AddProfileWorkerServices(builder.Configuration);

WebApplication app = builder.Build();
await app.RunAsync();
