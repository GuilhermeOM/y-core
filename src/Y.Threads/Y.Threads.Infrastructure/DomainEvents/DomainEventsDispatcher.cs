using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Infrastructure.DomainEvents;
internal sealed class DomainEventsDispatcher : IDomainEventsDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> _handlerTypeDictionary = new();
    private static readonly ConcurrentDictionary<Type, Type> _wrapperTypeDictionary = new();

    private readonly IServiceProvider _serviceProvider;

    public DomainEventsDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            using var scope = _serviceProvider.CreateScope();

            var domainEventType = domainEvent.GetType();

            var handlerType = _handlerTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(IDomainEventHandler<>).MakeGenericType(et));

            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler is not null)
            {
                var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);
                await handlerWrapper.Handle(domainEvent, cancellationToken);
            }
        }
    }

    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            var wrapperType = _wrapperTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(HandlerWrapper<>).MakeGenericType(et));

            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler)!;
        }
    }

    private sealed class HandlerWrapper<T> : HandlerWrapper where T : IDomainEvent
    {
        private readonly IDomainEventHandler<T> _handler;

        public HandlerWrapper(object handler)
        {
            _handler = (IDomainEventHandler<T>)handler;
        }

        public override async Task Handle(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await _handler.HandleAsync((T)domainEvent, cancellationToken);
        }
    }
}
