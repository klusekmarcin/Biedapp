using Biedapp.Application.DTOs;

namespace Biedapp.Application.Interfaces
{
    public interface IStatisticsService
    {
        Task<decimal> GetAverageMonthlyExpensesAsync(int monthsBack = 6);
        Task<Dictionary<string, decimal>> GetLast12MonthsBalanceAsync();
        Task<List<CategorySummaryDto>> GetTopExpenseCategoriesAsync(int topN = 5);
    }
}