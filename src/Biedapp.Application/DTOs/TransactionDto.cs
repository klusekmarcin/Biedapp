using Biedapp.Domain;
using Biedapp.Domain.Enums;

namespace Biedapp.Application.DTOs;
public record TransactionDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = DomainConstants.DefaultCurrencyCode;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }

    public string TypeDisplay => Type == TransactionType.Income ? "Income" : "Expense";
    public string AmountDisplay => Type == TransactionType.Income
        ? $"+{Amount:N2} {Currency}"
        : $"-{Amount:N2} {Currency}";
}