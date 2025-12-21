using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Abstractions.Behaviors;

namespace Y.Threads.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddUseCases()
            .AddDomainEvents()
            .AddValidators()
            .AddDecorators();
    }

    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IUseCaseHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IUseCaseHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        return services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);
    }

    public static IServiceCollection AddDecorators(this IServiceCollection services)
    {
        services.TryDecorate(typeof(IDomainEventHandler<>), typeof(LoggingDecorator.DomainEventHandler<>));

        services.TryDecorate(typeof(IUseCaseHandler<>), typeof(ValidationDecorator.ValidationHandler<>));
        services.TryDecorate(typeof(IUseCaseHandler<,>), typeof(ValidationDecorator.ValidationHandler<,>));

        services.TryDecorate(typeof(IUseCaseHandler<>), typeof(LoggingDecorator.UseCaseHandler<>));
        services.TryDecorate(typeof(IUseCaseHandler<,>), typeof(LoggingDecorator.UseCaseHandler<,>));

        return services;
    }
}
