using Biedapp.Domain;
using Biedapp.Domain.Enums;

namespace Biedapp.Application.Commands;
public record AddTransactionCommand
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = DomainConstants.DefaultCurrencyCode;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }

    public void Validate()
    {
        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(Amount));

        if (string.IsNullOrWhiteSpace(Category))
            throw new ArgumentException("Category is required", nameof(Category));

        if (string.IsNullOrWhiteSpace(Currency))
            throw new ArgumentException("Currency is required", nameof(Currency));

        if (Date > DateTime.Now.AddDays(1))
            throw new ArgumentException("Date cannot be in the future", nameof(Date));
    }
}