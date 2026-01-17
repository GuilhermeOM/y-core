using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Domain.Events;

public sealed record PostCreatedEvent(Guid PostId, Author Author, string Text, IReadOnlyCollection<Media> Medias) : IDomainEvent;
