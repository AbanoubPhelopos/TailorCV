using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TailorCV.CVGenerator.Contracts.Events;
using TailorCV.CVGenerator.Worker;
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
    opts.ListenToRabbitQueue("cv-generator.commands").MaximumParallelMessages(3);
    opts.PublishMessage<CVTailoringCompleted>().ToRabbitQueue("cv-generator.events");
    opts.PublishMessage<CVTailoringFailed>().ToRabbitQueue("cv-generator.events");
    opts.PublishMessage<CoverLetterCompleted>().ToRabbitQueue("cv-generator.events");
    opts.PublishMessage<CoverLetterFailed>().ToRabbitQueue("cv-generator.events");
    opts.PublishMessage<CvPdfExportCompleted>().ToRabbitQueue("cv-generator.events");
    opts.PublishMessage<CvPdfExportFailed>().ToRabbitQueue("cv-generator.events");
    opts.ApplicationAssembly = typeof(ModuleExtensions).Assembly;
    opts.ServiceName = "TailorCV.CVGenerator.Worker";
});

builder.Services.AddCVGeneratorWorkerServices(builder.Configuration);

WebApplication app = builder.Build();
await app.RunAsync();
