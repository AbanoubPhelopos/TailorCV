using Wolverine;
using Wolverine.RabbitMQ;

namespace TailorCV.JobDescriptions;

public class JobDescriptionsWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.PublishMessage<Contracts.Commands.ScrapeJobUrl>()
            .ToRabbitQueue("job-description.commands");
        options.PublishMessage<Contracts.Commands.ParseJobText>()
            .ToRabbitQueue("job-description.commands");

        options.ListenToRabbitQueue("job-description.events");
    }
}
