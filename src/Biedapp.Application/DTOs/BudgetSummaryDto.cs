using Biedapp.Domain;

namespace Biedapp.Application.DTOs;
public record BudgetSummaryDto
{
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; } = DomainConstants.DefaultCurrencyCode;
    public int TransactionCount { get; init; }
    public int IncomeCount { get; init; }
    public int ExpenseCount { get; init; }

    public string TotalIncomeDisplay => $"{TotalIncome:N2} {Currency}";
    public string TotalExpensesDisplay => $"{TotalExpenses:N2} {Currency}";
    public string BalanceDisplay => $"{Balance:N2} {Currency}";
    public string BalanceStatus => Balance >= 0 ? "Positive" : "Negative";
}
