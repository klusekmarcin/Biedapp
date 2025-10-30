using Biedapp.Application.DTOs;
using Biedapp.Application.Interfaces;
using Biedapp.Domain.Enums;

namespace Biedapp.Application.Services;
public class StatisticsService : IStatisticsService
{
    private readonly IBudgetService _budgetService;

    public StatisticsService(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    public async Task<Dictionary<string, decimal>> GetLast12MonthsBalanceAsync()
    {
        Dictionary<string, decimal> result = [];
        DateTime now = DateTime.Now;

        for (int i = 11; i >= 0; i--)
        {
            DateTime date = now.AddMonths(-i);
            Dictionary<string, decimal> monthData = await _budgetService.GetMonthlyIncomeExpensesAsync(date.Year, date.Month);
            result[$"{date:yyyy-MM}"] = monthData["Balance"];
        }

        return result;
    }

    public async Task<List<CategorySummaryDto>> GetTopExpenseCategoriesAsync(int topN = 5)
    {
        List<CategorySummaryDto> categories = await _budgetService.GetCategorySummaryAsync(TransactionType.Expense);
        return categories.Take(topN).ToList();
    }

    public async Task<decimal> GetAverageMonthlyExpensesAsync(int monthsBack = 6)
    {
        DateTime now = DateTime.Now;
        decimal total = 0;

        for (int i = 0; i < monthsBack; i++)
        {
            DateTime date = now.AddMonths(-i);
            Dictionary<string, decimal> monthData = await _budgetService.GetMonthlyIncomeExpensesAsync(date.Year, date.Month);
            total += monthData["Expenses"];
        }

        return total / monthsBack;
    }
}