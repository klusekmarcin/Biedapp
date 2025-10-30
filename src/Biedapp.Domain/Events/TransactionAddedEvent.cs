using Biedapp.Domain.Enums;

namespace Biedapp.Domain.Events;
public record TransactionAddedEvent : IEvent
{
    public Guid EventId { get; init; }
    public DateTime Timestamp { get; init; }
    public string EventType => nameof(TransactionAddedEvent);

    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = DomainConstants.DefaultCurrencyCode;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }

    public TransactionAddedEvent() { }
    public TransactionAddedEvent(
        Guid transactionId,
        decimal amount,
        string currency,
        string category,
        string description,
        TransactionType type,
        DateTime date)
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        TransactionId = transactionId;
        Amount = amount;
        Currency = currency;
        Category = category;
        Description = description;
        Type = type;
        Date = date;
    }
}