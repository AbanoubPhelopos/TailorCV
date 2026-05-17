using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System.Reflection;

namespace TailorCV.Shared.CQRS;

public static class CqrsRegistration
{
    public static void AddCQRSHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandLoggingDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryLoggingDecorator<,>));
    }

    private static void TryDecorate(
        IServiceCollection services,
        Type serviceType,
        Type decoratorType)
    {
        bool hasRegistration = services.Any(s => s.ServiceType.IsGenericType
            && s.ServiceType.GetGenericTypeDefinition() == serviceType);

        if (hasRegistration)
        {
            services.Decorate(serviceType, decoratorType);
        }
    }
}
