namespace Biedapp.Domain.Events;
public record TransactionDeletedEvent : IEvent
{
    public Guid EventId { get; init; }
    public DateTime Timestamp { get; init; }
    public string EventType => nameof(TransactionDeletedEvent);

    public Guid TransactionId { get; init; }

    public TransactionDeletedEvent() { }
    public TransactionDeletedEvent(Guid transactionId)
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        TransactionId = transactionId;
    }
}