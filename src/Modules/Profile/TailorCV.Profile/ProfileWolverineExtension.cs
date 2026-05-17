using Wolverine;
using Wolverine.RabbitMQ;

namespace TailorCV.Profile;

public class ProfileWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.PublishMessage<Contracts.Commands.ParseResume>()
            .ToRabbitQueue("profile.commands");

        options.PublishMessage<Contracts.Events.ProfileUpdated>()
            .ToRabbitQueue("profile.events");

        options.ListenToRabbitQueue("profile.events");
        options.ListenToRabbitQueue("identity.events");
    }
}
