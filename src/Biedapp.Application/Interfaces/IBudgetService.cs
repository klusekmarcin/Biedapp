using Biedapp.Application.Commands;
using Biedapp.Application.DTOs;
using Biedapp.Application.Queries;
using Biedapp.Domain.Enums;

namespace Biedapp.Application.Interfaces
{
    public interface IBudgetService
    {
        Task AddTransactionAsync(AddTransactionCommand command);
        Task DeleteTransactionAsync(DeleteTransactionCommand command);
        Task<List<string>> GetAllCategoriesAsync();
        Task<List<TransactionDto>> GetAllTransactionsAsync(GetTransactionsQuery query = null);
        Task<BudgetSummaryDto> GetBudgetSummaryAsync();
        Task<List<CategorySummaryDto>> GetCategorySummaryAsync(TransactionType? type = null);
        Task<int> GetEventCountAsync();
        Task<BudgetSummaryDto> GetMonthlyBudgetSummaryAsync(int year, int month);
        Task<Dictionary<string, decimal>> GetMonthlyIncomeExpensesAsync(int year, int month);
        string GetStorageLocation();
        Task<TransactionDto> GetTransactionByIdAsync(Guid id);
        Task UpdateTransactionAsync(UpdateTransactionCommand command);
    }
}