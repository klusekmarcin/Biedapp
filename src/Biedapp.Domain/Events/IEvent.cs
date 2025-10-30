using System.Text.Json.Serialization;

namespace Biedapp.Domain.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TransactionAddedEvent), "TransactionAdded")]
[JsonDerivedType(typeof(TransactionUpdatedEvent), "TransactionUpdated")]
[JsonDerivedType(typeof(TransactionDeletedEvent), "TransactionDeleted")]
public interface IEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
}
