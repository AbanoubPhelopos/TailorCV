using Wolverine;
using Wolverine.RabbitMQ;

namespace TailorCV.Identity;

public class IdentityWolverineExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.PublishMessage<Contracts.Events.UserRegistered>()
            .ToRabbitQueue("identity.events");

        options.PublishMessage<Contracts.Events.UserNameUpdated>()
            .ToRabbitQueue("identity.events");
    }
}
