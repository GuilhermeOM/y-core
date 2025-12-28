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
            .AddCommands()
            .AddQueries()
            .AddDomainEvents()
            .AddValidators()
            .AddDecorators();
    }

    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
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

        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandValidationHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandValidationHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(ValidationDecorator.QueryValidationHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        return services;
    }
}
