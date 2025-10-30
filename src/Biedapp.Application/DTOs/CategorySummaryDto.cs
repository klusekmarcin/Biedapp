using Biedapp.Domain;

namespace Biedapp.Application.DTOs;
public record CategorySummaryDto
{
    public string Category { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public int TransactionCount { get; init; }
    public string Currency { get; init; } = DomainConstants.DefaultCurrencyCode;
    public decimal Percentage { get; init; }

    public string TotalAmountDisplay => $"{TotalAmount:N2} {Currency}";
    public string PercentageDisplay => $"{Percentage:N1}%";
}
