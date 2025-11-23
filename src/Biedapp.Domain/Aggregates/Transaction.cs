using Biedapp.Domain.Enums;
using Biedapp.Domain.ValueObjects;

namespace Biedapp.Domain.Aggregates;
public sealed record Transaction
{
    public Guid Id { get; init; }
    public Money Amount { get; init; }
    public Category Category { get; init; }
    public string Description { get; init; }
    public TransactionType Type { get; init; }
    public DateOnly TransactionDate { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public Transaction(
        Guid id,
        Money amount,
        Category category,
        string description,
        TransactionType type,
        DateOnly transactionDate)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Transaction ID cannot be empty", nameof(id));

        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(category);

        Id = id;
        Amount = amount;
        Category = category;
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        TransactionDate = transactionDate;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public bool IsIncome() => Type == TransactionType.Income;
    public bool IsExpense() => Type == TransactionType.Expense;
}
