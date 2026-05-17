using Wolverine;
using Wolverine.RabbitMQ;

namespace TailorCV.CVGenerator;

public class CVGeneratorWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.PublishMessage<Contracts.Commands.TailorCV>()
            .ToRabbitQueue("cv-generator.commands");
        options.PublishMessage<Contracts.Commands.TailorCoverLetter>()
            .ToRabbitQueue("cv-generator.commands");
        options.PublishMessage<Contracts.Commands.ExportCvPdf>()
            .ToRabbitQueue("cv-generator.commands");

        options.ListenToRabbitQueue("cv-generator.events");
    }
}
